using ClinicBack.Context;
using ClinicBack.DTOS;
using ClinicBack.Entities;
using ClinicBack.Interfaces;
using com.sun.security.ntlm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Web.Http.Controllers;

namespace ClinicBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPhotoService _photoService;

        public UsersController(DataContext context, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IPhotoService photoService)
        {
            _context = context;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
            _photoService = photoService;
        }

        [HttpGet("GetDoctors")]
        public IActionResult GetAllDoctors() 
        {
            var doctors = _context.Doctors.ToList();
            return Ok(doctors);
        }
        [HttpGet("GetClients"), Authorize(Roles = "Client, Admin")]
        public IActionResult GetAllClients()
        {
            var clients = _context.Clients.ToList();
            return Ok(clients);
        }

        [HttpGet("GetSingleDoctor/{id}")]
        public IActionResult GetDoctor(int id) 
        {
            var doctor = _context.Doctors.SingleOrDefault(x => x.Id == id);
            return Ok(doctor);
        }

        [HttpGet("getloginuser/{email}")]
        public async Task<IActionResult> GetLoginUserAsync(string email) 
        {
            var client = await _context.Clients.SingleOrDefaultAsync(x => x.Email == email);

            if (client != null)
            {
                return Ok(client);
            }

            var doctor = await _context.Doctors.SingleOrDefaultAsync(x => x.Email == email);

            if (doctor != null)
            {
                return Ok(doctor);
            }

            var admin = await _context.Admins.SingleOrDefaultAsync(x => x.Email == email);

            if (admin != null)
            {
                return Ok(admin);
            }

            return Unauthorized("Email or Phone is incorrect");
        }


        [HttpGet("GetProfileDates/{Role}/{Id}")]
        public ActionResult<ProfileDatesDto> GetProfileDates(string Role, string Id)
        {
            if(Role == "Client")
            {
                var times = _context.Times.Where(x => x.ClientId == Id).Select(x => x.Date).ToList();
                return new ProfileDatesDto { Times = times };
            }
            else
            {
                var times = _context.Times.Where(x => x.DoctorId == Id).Select(x => x.Date).ToList();
                return new ProfileDatesDto { Times = times };
            }
            
        }

        [HttpPost("AddDate"), Authorize(Roles ="Client, Admin")]
        public async Task<IActionResult> AddDateAsync([FromForm] TimeDto time)
        {
            if(await DateExist(time)) return BadRequest("Date is reserved");
            if(await HaveReservedDateOtherDoctor(time)) return BadRequest("You have reserve ");
            if (time.Date == null) { return BadRequest(); }
            
            var date = new Times
            {
                DoctorId = time.DoctorId,
                ClientId = time.ClientId,
                Date = time.Date,
                Problem = time.Problem
            };
            _context.Times.Add(date);
            _context.SaveChanges();
            return Ok(date);
        }

        [HttpPost("DeleteDate"), Authorize(Roles = "Client, Admin")]
        public IActionResult DeleteDate([FromForm] TimeDto time)
        {
            if (time.Date == null) { return BadRequest(); }
            var date = _context.Times.First(x => x.Date == time.Date && x.ClientId == time.ClientId && x.DoctorId == time.DoctorId);
            _context.Times.Remove(date);
            _context.SaveChanges();
            return Ok();
        }

        [HttpPost("deletedatefromprofile"), Authorize(Roles = "Client, Doctor, Admin")]
        public IActionResult DeletedateFromProfile([FromForm] TimeDto time)
        {
            if (time.Date == null) { return BadRequest(); }
            if (time.DoctorId == null)
            {
                var date = _context.Times.First(x => x.Date == time.Date && x.ClientId == time.ClientId);
                _context.Times.Remove(date);
            }
            if (time.ClientId == null)
            {
                var date = _context.Times.First(x => x.Date == time.Date && x.DoctorId == time.DoctorId);
                _context.Times.Remove(date);
            }
            _context.SaveChanges();
            return Ok();
        }
        [HttpGet("GetReservedTimes/{clientid}/{doctorid}")]
        public ActionResult<DatesDto> GetReservedDateTimes(string clientid, string doctorid)
        {
            var dates = _context.Times.Where(x => x.ClientId == clientid && x.DoctorId == doctorid).Select(x => x.Date).ToList();
            var reserveddates = _context.Times.Where(x => x.ClientId != clientid && x.DoctorId == doctorid).Select(x => x.Date).ToList();
            var reservedWithOtherDoctor = _context.Times.Where(x => x.ClientId == clientid && x.DoctorId != doctorid).Select(x => x.Date).ToList();
            return new DatesDto { ClientDates = dates, ReservedDates = reserveddates, ReservedWithOther = reservedWithOtherDoctor };
        }

        [HttpGet("GetDoctorTimes/{doctorid}"), Authorize(Roles ="Client")]
        public ActionResult<DatesDto> GetDoctorTimes(string doctorid)
        {
            var dates = _context.Times.Where(x => x.DoctorId == doctorid).Select(x => x.Date).ToList();
            return new DatesDto { ClientDates = dates };
        }



        [HttpGet("GetDoctorsByCategory/{category}"), Authorize(Roles ="Client, Doctors")]
        public IActionResult GetDoctorsByCat(string category)
        {
            var doctors = _context.Doctors.Where(x => x.Categories == category).ToList();
            return Ok(doctors);
        }

        [HttpGet("GetAllUsers"), Authorize(Roles ="Admin")]
        public IActionResult GetAllData()
        {
            var doctors = _context.Doctors.ToList();
            var clients = _context.Clients.ToList();
            var combinedList = doctors.Cast<object>().Concat(clients).ToList();
            return Ok(combinedList);
        }

        [HttpPut("Edituser"), Authorize(Roles ="Admin")]
        public async Task<IActionResult> ChangeUser([FromForm] EditData data)
        {
            string defPhoto = null; // Initialize defPhoto as string instead of dynamic

            if (Request.Form.Files.Count < 1)
            {
                if (data.Role == "Client")
                {
                    var client = _context.Clients.FirstOrDefault(x => x.Id == data.Id);
                    defPhoto = client?.Photo; // Extract the photo value from the query result
                }
                else if (data.Role == "Doctor")
                {
                    var doctor = _context.Doctors.FirstOrDefault(x => x.Id == data.Id);
                    defPhoto = doctor?.Photo; // Extract the photo value from the query result
                }
            }
            else
            {
                var request1 = Request.Form.Files[0];
                var result = await _photoService.AddPhotoAsync(request1);
                defPhoto = result.SecureUrl.AbsoluteUri;
            }

            if (data.Role == "Client")
            {
                var person = _context.Clients.Find(data.Id);
                person.Photo = defPhoto;
                person.Firstname = data.Firstname;
                person.Lastname = data.Lastname;
                person.Categories = data.Categories;

                await _context.SaveChangesAsync();
            }
            else if (data.Role == "Doctor")
            {
                var person = _context.Doctors.Find(data.Id);
                person.Photo = defPhoto;
                person.Firstname = data.Firstname;
                person.Lastname = data.Lastname;
                person.Categories = data.Categories;

                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpGet("IncreaseView/{id}")]
        public async Task<IActionResult> IncreaseView(int id)
        {
            var person = this._context.Doctors.Find(id);
            person.Views += 1;
            await this._context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("Delete/user/{role}/{id}"), Authorize(Roles ="Admin")]
        public IActionResult DeleteUser(string role,int id) 
        {
            if(role == "Client")
            {
                var clientToDelete = _context.Clients.FirstOrDefault(c => c.Id == id);
                _context.Clients.Remove(clientToDelete);
                var rowsToDelete = _context.Times.Where(t => t.ClientId == id.ToString());
                _context.Times.RemoveRange(rowsToDelete);
            }
            else if(role == "Doctor")
            {
                var clientToDelete = _context.Doctors.FirstOrDefault(c => c.Id == id);
                _context.Doctors.Remove(clientToDelete);
                var rowsToDelete = _context.Times.Where(t => t.DoctorId == id.ToString());
                _context.Times.RemoveRange(rowsToDelete);
            } 
                _context.SaveChanges();
            return Ok("Deleted succesfully");
        }

        private async Task<bool> DateExist(TimeDto date) { return await _context.Times.AnyAsync(row => row.DoctorId == date.DoctorId && row.Date == date.Date && row.ClientId != date.ClientId); }
        private async Task<bool> HaveReservedDateOtherDoctor(TimeDto date) { return await _context.Times.AnyAsync(row => row.ClientId == date.ClientId && row.Date == date.Date && row.DoctorId != date.DoctorId); }

    }
}
