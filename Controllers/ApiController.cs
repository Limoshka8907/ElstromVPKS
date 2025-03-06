using ElstromVPKS.DbRequest;
using ElstromVPKS.JWT;
using ElstromVPKS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ElstromVPKS.Controllers
{
    [Route("api/[controller]")]
    // Scaffold-DbContext "Data Source=DESKTOP-IM6F3Q9;Initial Catalog=ElstromTest;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Tables tests -Force

    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ElstromContext _db;
        private JwtProvider _jwtProvider;
        private readonly UserRequests _userRequests;

        public UserController(ElstromContext appDBContext, JwtProvider jwtProvider)
        {
            _db = appDBContext;
            _jwtProvider = jwtProvider;
            _userRequests = new UserRequests(_db);
        }

        // Получение всех гостей  
        [HttpGet]
        [Authorize]

        [Route("/getEmployees")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
        {
            return await _db.Employees.ToListAsync();
        }

        // Получение гостя по ID  
        [HttpGet]
        [Route("/getEmployees/{id}")]
        public async Task<ActionResult<Employee>> GetEmployeeById(int id)
        {
            var Employee = await _db.Employees.FindAsync(id);

            if (Employee == null)
            {
                return NotFound("Гость с данным ID не найден");
            }

            return Employee;
        }

        // Добавление нового гостя  
        [HttpPost]
        [Route("/addEmployee")]
        public async Task<ActionResult<Employee>> CreateEmployee([FromBody] Employee Employee)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _db.Employees.Add(Employee);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmployeeById), new { id = Employee.Id }, Employee);
        }

        // Обновление данных гостя  
        [HttpPut("/updateEmployee/{id}")]
        public async Task<IActionResult> UpdateEmployee(Guid id, Employee Employee)
        {
            if (id != Employee.Id)
            {
                return BadRequest();
            }

            _db.Entry(Employee).State = EntityState.Modified;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(id))
                {
                    return NotFound("Гость с таким ID не найден");
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        private bool EmployeeExists(Guid id)
        {
            return _db.Employees.Any(e => e.Id == id);
        }

        // Удаление гостя  
        [HttpDelete]
        [Route("/deleteEmployee/{id}")]
        public async Task<IActionResult> DeleteEmployee(Guid id)
        {
            var Employee = await _db.Employees.FindAsync(id);
            if (Employee == null)
            {
                return NotFound();
            }

            _db.Employees.Remove(Employee);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost]
        [Route("/login")]

        public async Task<string> Login([FromBody] LoginRequest request)
        {
            var user = await _db.Employees.FirstOrDefaultAsync(u => u.Username == request.Login);
            if (user == null || user.HashedPassword != request.Password)
            {
                throw new Exception("Failed to login");
            }

            var token = _jwtProvider.GenerateToken(user);
            HttpContext.Response.Cookies.Append("authToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });


            return token;
        }

        [HttpPost]
        [Route("/customers/login")]

        public async Task<string> CustomersLogin([FromBody] LoginRequestCustomer request)
        {
            var user = await _db.Customers.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || user.HashedPassword != request.Password)
            {
                throw new Exception("Failed to login");
            }

            var token = _jwtProvider.GenerateTokenCustomer(user);
            HttpContext.Response.Cookies.Append("authTokenCustomer", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });


            return token;
        }


        // Создаём модель для запроса:
        public class LoginRequest
        {
            public string? Login { get; set; }
            public string? Password { get; set; }
        }
        public class LoginRequestCustomer
        {
            public string? Email { get; set; }
            public string? Password { get; set; }
        }

        [HttpGet("/auth-check")]
        public IActionResult CheckAuth()
        {
            var token = Request.Cookies["authToken"];
            Console.WriteLine(token);
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            return Ok();
        }

        [HttpGet("/customers/auth-check")]
        public IActionResult CheckAuthCusomter()
        {
            var token = Request.Cookies["authTokenCustomer"];
            Console.WriteLine(token);
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            return Ok();
        }

        [HttpGet]
        [Route("/getTests")]
        public async Task<ActionResult<IEnumerable<Test>>> GetTests()
        {

            return await _db.Tests.ToListAsync();
        }

        [HttpGet]
        [Route("/getActions")]
        public async Task<ActionResult<IEnumerable<TestView>>> GetActions()
        {

            return await _db.TestViews.ToListAsync();
        }



        private bool VerifyPassword(string password, string hashedPassword)
        {
            // Заменить на свою проверку хеша (например, с BCrypt)
            return password == hashedPassword;
        }

        // POST: api/Test/addTest
        [HttpPost("/addTest")]
        public async Task<IActionResult> AddTest([FromForm] TestCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var test = new Test
            {
                TestName = request.TestName,
                TestType = request.TestType,
                // = request.Description,
                Status = request.Status,
                Parametrs = request.Parameters
                // Если у вас есть дополнительные поля, например, параметры, обрабатывайте их здесь
            };

            var action = new TestView
            {
                CustomerId = Guid.Parse("142B1864-8AE1-47C9-AF2D-0EA46814A77E"),
                TestId = test.Id,
                ViewedAt = DateTime.Now
            };
            _db.Tests.Add(test);
            await _db.SaveChangesAsync();

            _db.TestViews.Add(action);
            await _db.SaveChangesAsync();


            

            // Если необходимо, можно обработать загрузку файлов:
            // if (request.Files != null)
            // {
            //     foreach (var file in request.Files)
            //     {
            //         // Логика сохранения файла, например, сохранение в папку или в БД
            //     }
            // }

            return Ok(new { message = "Test created successfully", testId = test.Id });
        }

        // Получение деталей теста по его ID
        [HttpGet("/getTestById/{id}")]
        public async Task<IActionResult> GetTestById(Guid id)
        {
            var test = await _db.Tests.FindAsync(id);
            if (test == null)
            {
                return NotFound(new { message = "Тест не найден" });
            }
            return Ok(test);
        }

        
    }

    // Модель запроса для создания теста
    public class TestCreateRequest
    {
        public string? TestName { get; set; }
        public string? TestType { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        // Если нужно передавать дополнительные параметры, например, в формате JSON:
        public string? Parameters { get; set; }
        public IFormFileCollection? Files { get; set; }
    }
}
   

