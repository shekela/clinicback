using ClinicBack.Context;
using ClinicBack.DTOS;
using ClinicBack.Entities;
using ClinicBack.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPhotoService _photoService;

        public AdminController(DataContext context, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IPhotoService photoService)
        {
            _context = context;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
            _photoService = photoService;
        }

        [HttpGet("GetUserProfile/{role}/{id}"), Authorize(Roles ="Admin")]
        public IActionResult GetUserProfile(string role, int id) 
        {
            var user = (dynamic)null;
            if (role == "Client") 
            {
               var client = _context.Clients.Where(x => x.Id == id).FirstOrDefault();
               user = client;
            }
            else if(role == "Doctor")
            {
                var client = _context.Doctors.Where(x => x.Id == id).FirstOrDefault();
                user = client;
            }
            return Ok(user);
        }

        [HttpPost("AddDate"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddDateAsync([FromForm] TimeDto time)
        {
            if (await DateExistForClient(time)) return BadRequest("Date is reserved");
            if (await DateExistForDoctor(time)) return BadRequest("Date is reserved");
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
        [HttpGet("On/{id}")]
        public IActionResult Securityon(int id)
        {
            var clients = _context.Clients.FirstOrDefault(x => x.Id == id); // Retrieve all Client entities

            clients.Security = "ON";

            _context.SaveChanges();

            return Ok(clients);

        }
        private async Task<bool> DateExistForClient(TimeDto date) 
        { 
            return await _context.Times.AnyAsync(row => row.ClientId == date.ClientId && row.Date == date.Date); 
        }
        private async Task<bool> DateExistForDoctor(TimeDto date)
        {
            return await _context.Times.AnyAsync(row => row.DoctorId == date.DoctorId && row.Date == date.Date);
        }


    }
}
