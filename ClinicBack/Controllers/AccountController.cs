using ClinicBack.Context;
using ClinicBack.DTOS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using ClinicBack.Entities;
using System.Security.Cryptography;
using System.Text;
using ClinicBack.Interfaces;
using Microsoft.EntityFrameworkCore;
using javax.jws;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System;
using static com.sun.tools.@internal.xjc.reader.xmlschema.bindinfo.BIConversion;
using javax.xml.transform;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using sun.rmi.server;

namespace ClinicBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        private readonly IPhotoService _photoService;

        public AccountController(DataContext context, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IPhotoService photoService)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _tokenService = tokenService;
            _photoService = photoService;
        }

        [HttpGet("send-email-otp/{email}")]
        public IActionResult Sendmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email address cannot be null or empty.", nameof(email));
            }

            Random rnd = new Random();
            string otpCode = rnd.Next(100000, 999999).ToString();

            HttpContext.Session.SetString("OTPCode", otpCode);
            HttpContext.Session.SetString("Email", email);
            HttpContext.Session.SetString("OTPTime", DateTime.Now.ToString());

            SendEmail(email, "Your OTP code", $"Your OTP code is {otpCode}");

            return Ok();
        }

        [HttpGet("send-reset-otp/{email}")]
        public IActionResult SendResetOTP(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email address cannot be null or empty.", nameof(email));
            }

            Random rnd = new Random();
            string otpCode = rnd.Next(100000, 999999).ToString();

            HttpContext.Session.SetString("ResetCode", otpCode);
            HttpContext.Session.SetString("ResetEmail", email);
            HttpContext.Session.SetString("ResetOTPTime", DateTime.Now.ToString());

            SendEmail(email, "Your OTP code", $"Your OTP code is {otpCode}");

            return Ok();
        }

        [HttpPut("ChangePhoto/{id}")]
        public async Task<IActionResult> ChangeClientPhoto(int id, [FromForm] Photo picture)
        {
            if (Request.Form.Files.Count < 1)
            {
                return BadRequest("Files are required.");
            }
            var request1 = Request.Form.Files[0];

            var person = this._context.Clients.Find(id);
            var result = await _photoService.AddPhotoAsync(request1);
            var photoRes = result.SecureUrl.AbsoluteUri;
            person.Photo = photoRes;
            await this._context.SaveChangesAsync();
            return Ok();
        }

        [WebMethod]
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto logindto)
        {
            var user = (dynamic)null;
            var user1 = await _context.Clients.SingleOrDefaultAsync(x => x.Email == logindto.Email);
            var user2 = await _context.Doctors.SingleOrDefaultAsync(x => x.Email == logindto.Email);
            var user3 = await _context.Admins.SingleOrDefaultAsync(x => x.Email == logindto.Email);
            string token;

            if (user1 == null)
            {
                if (user2 == null)
                {
                    if (user3 == null)
                    {
                        return Unauthorized("Email or Phone is incorrect");
                    }
                    user = user3;
                }
                else
                {
                    user = user2;
                }
            }
            else
            {
                user = user1;
            }

            if (user == null) // Additional null check
            {
                return Unauthorized("User not found");
            }

            if(user.Security == "OFF")
            {
                using var hmac = new HMACSHA512(user.PasswordSalt);

                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(logindto.Password));

                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != user.PasswordHash[i])
                    {
                        return Unauthorized("Password is incorrect");
                    }
                }

                if (user.Role == "Doctor")
                {
                    token = _tokenService.CreateTokenDoctor(user);
                }
                else if (user.Role == "Client")
                {
                    token = _tokenService.CreateToken(user);
                }
                else
                {
                    token = _tokenService.CreateTokenAdmin(user);
                }

                return new UserDto { Email = user.Email, Token = token };
            }

            else if (user.Security == "ON")
            {
                var email = HttpContext.Session.GetString("Email");
                if (string.IsNullOrEmpty(email)) return BadRequest("Email empty");
                if (!ConfirmOTP(logindto.OtpCode)) return BadRequest("Incorrect otp");
                if (logindto.Email != email) return BadRequest("You need to verify YOUR email " + email);


                using var hmac = new HMACSHA512(user.PasswordSalt);

                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(logindto.Password));

                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != user.PasswordHash[i])
                    {
                        return Unauthorized("Password is incorrect");
                    }
                }

                if (user.Role == "Doctor")
                {
                    token = _tokenService.CreateTokenDoctor(user);
                }
                else if (user.Role == "Client")
                {
                    token = _tokenService.CreateToken(user);
                }
                else
                {
                    token = _tokenService.CreateTokenAdmin(user);
                }

                return new UserDto { Email = user.Email, Token = token };
            }
            // Default return statement if the code does not meet the conditions above
            return Unauthorized("Invalid security value");
        }

        [WebMethod]
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterDto registerDto)
        {
            var email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(email)) return BadRequest("Email empty");
            if (!ConfirmOTP(registerDto.Otp)) return BadRequest("Incorrect otp");
        
            
            if (await UserExist(registerDto.Email.ToLower())) return BadRequest("Email is taken, use other email");
            if (await DoctorExist(registerDto.Email.ToLower())) return BadRequest("Email is taken, use other email");
            if (await AdminExist(registerDto.Email.ToLower())) return BadRequest("Email is taken, use other email");

            if (registerDto.Email != email) return BadRequest("You need to verify YOUR email " + email);

            HttpContext.Session.Remove("OTPCode");
            HttpContext.Session.Remove("Email");
            HttpContext.Session.Remove("OTPTime");


            using var hmac = new HMACSHA512();

            var user = new Client
            {
                Firstname = registerDto.Firstname.ToLower(),
                Lastname = registerDto.Lastname.ToLower(),
                Idnumber = registerDto.Idnumber,
                Email= registerDto.Email.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key,
                Photo = "https://res.cloudinary.com/dqq768hoa/image/upload/v1685278376/da-net7/lu0mkngxcrsulmmjgdyf.png",
                Role = "Client",
                Security = "OFF",
                Categories = "მომხმარებელი"
            };

            await _context.Clients.AddAsync(user);
            await _context.SaveChangesAsync();

            return new UserDto { Email = user.Email, Firstname = user.Firstname, Lastname = user.Lastname, Idnumber = user.Idnumber, Role = user.Role, Token = _tokenService.CreateToken(user) };
        }

        [HttpPost("register-doctor")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
        public async Task<ActionResult<UserDto>> RegisterDoctor([FromForm] DoctorRegisterDto registerDto)
        {
            if (await UserExist(registerDto.Email.ToLower())) return BadRequest("Email is taken, use other email");
            if (await DoctorExist(registerDto.Email.ToLower())) return BadRequest("Email is taken, use other email");
            if (await AdminExist(registerDto.Email.ToLower())) return BadRequest("Email is taken, use other email");

            if (Request.Form.Files.Count < 2)
            {
                return BadRequest("Two files are required.");
            }

            var request1 = Request.Form.Files[0];
            var request2 = Request.Form.Files[1];
            var resultP = await _photoService.AddPhotoAsync(request1);
            var resultF = await _photoService.AddPhotoAsync(request2);

            var photo = resultP.SecureUrl.AbsoluteUri;
            var cvfile = resultF.SecureUrl.AbsoluteUri;

            using var hmac = new HMACSHA512();

            var user = new Doctor
            {
                Firstname = registerDto.Firstname.ToLower(),
                Lastname = registerDto.Lastname.ToLower(),
                Idnumber = registerDto.Idnumber,
                Email = registerDto.Email.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key,
                Categories = registerDto.Categories,
                Views = 0,
                Photo = photo,
                Cvfile = cvfile,
                Description = "ექიმი",
                Role = "Doctor",
                Security = "OFF"
            };
            await _context.Doctors.AddAsync(user);
            await _context.SaveChangesAsync();

            return new UserDto { Email = user.Email, Firstname = user.Firstname, Lastname = user.Lastname, Idnumber = user.Idnumber, Role = user.Role, Token = _tokenService.CreateTokenDoctor(user) };
        }

        [HttpPost("register-admin")]
        public async Task<ActionResult<UserDto>> RegisterAdmin([FromForm] AdminDto registerDto)
        {
            if (await UserExist(registerDto.Email.ToLower())) return BadRequest("Email is taken, use other email");
            if (await DoctorExist(registerDto.Email.ToLower())) return BadRequest("Email is taken, use other email");
            if (await AdminExist(registerDto.Email.ToLower())) return BadRequest("Email is taken, use other email");

            using var hmac = new HMACSHA512();
            var user = new Admin
            {
                Email = registerDto.Email.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key,
                Photo = "https://res.cloudinary.com/dqq768hoa/image/upload/v1685278376/da-net7/lu0mkngxcrsulmmjgdyf.png",
                Role = "Admin",
                Security = "OFF"
            };
            await _context.Admins.AddAsync(user);
            await _context.SaveChangesAsync();

            return new UserDto { Email = user.Email, Token = _tokenService.CreateTokenAdmin(user) };
        }

        [WebMethod]
        [HttpPost("ResetPassword")]
        public ActionResult<UserDto> ResetPassword([FromBody] ResetDto resetDto)
        {
            var email = HttpContext.Session.GetString("ResetEmail");
            if (!ConfirmResetCode(resetDto.OtpR)) return Ok("Incorrect OTP");
            if (string.IsNullOrEmpty(email)) return Ok("Email is empty");

            if (resetDto.EmailR != email) return BadRequest("You need to verify YOUR email " + email);

            HttpContext.Session.Remove("ResetCode");
            HttpContext.Session.Remove("ResetEmail");
            HttpContext.Session.Remove("ResetOTPTime");


            using var hmac = new HMACSHA512();

            var User = this._context.Clients.First(x => x.Email == resetDto.EmailR);
            User.Email = email;
            User.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(resetDto.PasswordR));
            User.PasswordSalt = hmac.Key;
            this._context.SaveChanges();
            return new UserDto { Email = User.Email, Token = _tokenService.CreateToken(User)};
        }

        private bool ConfirmOTP(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return false;
            }
            string sessionOTP = HttpContext.Session.GetString("OTPCode");
            string sessionTimeString = HttpContext.Session.GetString("OTPTime");


            if (sessionOTP != code)
            {
                return false;
            }

            DateTime sessionTime;
            if (!DateTime.TryParse(sessionTimeString, out sessionTime))
            {
                return false;
            }

            if ((DateTime.Now - sessionTime).TotalMinutes > 30)
            {
                return false;
            }
            return true;
        }
        private bool ConfirmResetCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return false;
            }
            string sessionOTP = HttpContext.Session.GetString("ResetCode");
            string sessionTimeString = HttpContext.Session.GetString("ResetOTPTime");


            if (sessionOTP != code)
            {
                return false;
            }

            DateTime sessionTime;
            if (!DateTime.TryParse(sessionTimeString, out sessionTime))
            {
                return false;
            }

            if ((DateTime.Now - sessionTime).TotalMinutes > 30)
            {
                return false;
            }
            return true;
        }
        private async Task<bool> UserExist(string email)
        {
            return await _context.Clients.AnyAsync(x => x.Email == email.ToLower());
        }
        private async Task<bool> AdminExist(string email)
        {
            return await _context.Admins.AnyAsync(x => x.Email == email.ToLower());
        }
        private async Task<bool> DoctorExist(string email)
        {
            return await _context.Doctors.AnyAsync(x => x.Email == email.ToLower());
        }
        private static void SendEmail(string email, string subject, string body)
        {
            var fromAddress = new MailAddress("lukashekelashvili@gmail.com", "Clinic");
            var toAddress = new MailAddress(email);
            const string fromPassword = "bnatgrawbmauhjed";
            const string smtpServer = "smtp.gmail.com";

            var smtp = new SmtpClient
            {
                Host = smtpServer,
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                smtp.Send(message);
            }
        }

        [HttpGet("Check2FA/{email}")]
        public async Task<ActionResult<SecResponse>> Check2FA(string email)
        {
            var user = (dynamic)null;
            var user1 = await _context.Clients.SingleOrDefaultAsync(x => x.Email == email);
            var user2 = await _context.Doctors.SingleOrDefaultAsync(x => x.Email == email);
            var user3 = await _context.Admins.SingleOrDefaultAsync(x => x.Email == email);

            if (user1 == null)
            {
                if (user2 == null)
                {
                    if (user3 == null)
                    {
                        return Unauthorized("Email or Phone is incorrect");
                    }
                    user = user3;
                }
                else
                {
                    user = user2;
                }
            }
            else
            {
                user = user1;
            }

            if (user == null) // Additional null check
            {
                return Unauthorized("User not found");
            }

            if (user.Role != "Client")
            {
                return new SecResponse { Response = "OFF" };
            }

            if (user.Security == "ON")
            {
                Random rnd = new Random();
                string otpCode = rnd.Next(100000, 999999).ToString();
                
                HttpContext.Session.SetString("OTPCode", otpCode);
                HttpContext.Session.SetString("Email", email);
                HttpContext.Session.SetString("OTPTime", DateTime.Now.ToString());

                SendEmail(email, "Your OTP code", $"Your OTP code is {otpCode}");
                return new SecResponse { Response = "ON" };
            }
            else
            {
                return new SecResponse { Response = "OFF" };
            }
        }

        [HttpPost("Change2FA")]
        public async Task<ActionResult<SecResponse>> ChangeUser2FA([FromForm] SecurityChangeDto data)
        {
            var user = (dynamic)null;
            var user1 = await _context.Clients.SingleOrDefaultAsync(x => x.Role == data.Role && x.Id == data.Id);
            var user2 = await _context.Doctors.SingleOrDefaultAsync(x => x.Role == data.Role && x.Id == data.Id);
            var user3 = await _context.Admins.SingleOrDefaultAsync(x => x.Role == data.Role && x.Id == data.Id);

            if (user1 == null)
            {
                if (user2 == null)
                {
                    if (user3 == null)
                    {
                        return Unauthorized("Email or Phone is incorrect");
                    }
                    user = user3;
                }
                else
                {
                    user = user2;
                }
            }
            else
            {
                user = user1;
            }
            if (user == null) // Additional null check
            {
                return Unauthorized("User not found");
            }
            if(user.Security == "ON")
            {
                user.Security = "OFF";
                _context.SaveChanges();
                return new SecResponse { Response = user.Security };
            }
            else if(user.Security == "OFF"){
                user.Security = "ON";
                _context.SaveChanges();
                return new SecResponse { Response = user.Security };
            }
            return new SecResponse { Response = user.Security };
        }
        [HttpGet("GetSecurity/{role}/{id}")]
        public async Task<ActionResult<SecResponse>> GetSecurity(string role, int id)
        {
            string security = "OFF";

            if (role == "Client")
            {
                var client = await _context.Clients.SingleOrDefaultAsync(x => x.Id == id);
                if (client != null)
                {
                    security = client.Security;
                }
            }
            else if (role == "Doctor")
            {
                var doctor = await _context.Doctors.SingleOrDefaultAsync(x => x.Id == id);
                if (doctor != null)
                {
                    security = doctor.Security;
                }
            }
            else if (role == "Admin")
            {
                var admin = await _context.Admins.SingleOrDefaultAsync(x => x.Id == id);
                if (admin != null)
                {
                    security = admin.Security;
                }
            }

            return new SecResponse { Response = security};
        }
    }
}
    
