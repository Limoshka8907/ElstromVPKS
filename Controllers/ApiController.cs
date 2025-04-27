using ElstromVPKS.DbRequest;
using ElstromVPKS.JWT;
using ElstromVPKS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ElstromVPKS.Controllers
{
    [Route("api/[controller]")]
    // Scaffold-DbContext "Data Source=DESKTOP-IM6F3Q9;Initial Catalog=ElstromTest;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Tables tests -Force
    //dotnet ef dbcontext scaffold "Data Source=DESKTOP-IM6F3Q9;Initial Catalog=ElstromTest;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context ElstromContext --force --no-build --verbose
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

        //[Route("/getEmployees")]
        //public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
        //{
        //    return await _db.Employees.ToListAsync();
        //}

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

        //// Добавление нового гостя  
        //[HttpPost]
        //[Route("/addEmployee")]
        //public async Task<ActionResult<Employee>> CreateEmployee([FromBody] Employee Employee)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    _db.Employees.Add(Employee);
        //    await _db.SaveChangesAsync();

        //    return CreatedAtAction(nameof(GetEmployeeById), new { id = Employee.Id }, Employee);
        //}

        [HttpGet]
        [Authorize(Roles = "Глава инженерного отдела")]
        [Route("/getEmployees")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
        {
            return await _db.Employees.Where(e => e.DeletedAt == null).ToListAsync();
        }

        [HttpGet]
        [Authorize(Roles = "Глава инженерного отдела,Инженер")]
        [Route("/getCustomers")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            return await _db.Customers.Where(e => e.DeletedAt == null).ToListAsync();
        }

        [HttpPost]
        [Authorize(Roles = "Глава инженерного отдела,Инженер")]
        [Route("/addEmployee")]
        public async Task<ActionResult<Employee>> CreateEmployee([FromBody] Employee employee)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            employee.Id = Guid.NewGuid();
            employee.CreatedAt = DateTime.Now;
            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmployeeById), new { id = employee.Id }, employee);
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
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.Now.AddHours(1)
            });


            return token;
        }

        [HttpPost("/logout")]
        public IActionResult Logout()
        {
            HttpContext.Response.Cookies.Delete("authToken");
            return Ok();
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

            CustomerStorage.Customer = user;

            var token = _jwtProvider.GenerateTokenCustomer(user);
            HttpContext.Response.Cookies.Append("authTokenCustomer", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });


            return token;
        }

        [HttpPost("customer/logout")]
        public IActionResult CustomerLogout()
        {
            HttpContext.Response.Cookies.Delete("authTokenCustomer");
            return Ok();
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
        [Authorize(Roles = "Глава инженерного отдела,Инженер")]
        [Route("/getTests")]
        public async Task<ActionResult<IEnumerable<Test>>> GetTests()
        {

            return await _db.Tests.ToListAsync();
        }

        [HttpGet]
        [Authorize] // Только для аутентифицированных пользователей
        [Route("/getCustomerTests")]
        public async Task<ActionResult<IEnumerable<object>>> GetCustomerTests()
        {

            // Извлекаем userId из токена
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Невалидный пользователь");
            }

            // Проверяем, что пользователь — клиент
            var customerExists = await _db.Customers.AnyAsync(c => c.Id == userId);
            if (!customerExists)
            {
                return Forbid("Пользователь не является клиентом");
            }

            // Получаем тесты, назначенные клиенту
            var tests = await _db.TestCustomerAssignments
                .Where(tca => tca.CustomerId == userId)
                .Select(tca => new
                {
                    Id = tca.Test.Id,
                    TestName = tca.Test.TestName,
                    TestType = tca.Test.TestType,
                    Status = tca.Test.Status,
                    CreatedAt = tca.Test.CreatedAt,
                    Parametrs = tca.Test.Parametrs,
                    AssignedAt = tca.AssignedAt
                })
                .ToListAsync();

            return Ok(tests);
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
        [Authorize(Roles = "Глава инженерного отдела,Инженер")]
        public async Task<IActionResult> AddTest([FromForm] TestCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToList();
                return BadRequest(new { message = "Validation failed", errors });
            }

            var employeeId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var test = new Test
            {
                TestName = request.TestName,
                TestType = request.TestType,
                //Description = request.Description,
                Status = request.Status,
                Parametrs = request.Parameters ?? "[]", // Пустой JSON, если Parameters отсутствует
                CreatedAt = DateTime.Now
            };

            _db.Tests.Add(test);
            await _db.SaveChangesAsync();

            // Назначение теста клиенту
            if (!string.IsNullOrEmpty(request.CustomerId) && Guid.TryParse(request.CustomerId, out var customerId))
            {
                var customer = await _db.Customers.FindAsync(customerId);
                if (customer == null)
                {
                    return BadRequest("Клиент не найден");
                }

                var assignment = new TestCustomerAssignment
                {
                    TestId = test.Id,
                    CustomerId = customerId,
                    AssignedBy = employeeId,
                    AssignedAt = DateTime.Now
                };
                _db.TestCustomerAssignments.Add(assignment);
            }

            // Обработка файлов
            if (request.Files != null && request.Files.Any())
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                foreach (var file in request.Files)
                {
                    if (file.Length > 0)
                    {
                        var filePath = Path.Combine(uploadsDir, $"{Guid.NewGuid()}_{file.FileName}");
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        // Если таблица test_files создана:
                        // var testFile = new TestFile
                        // {
                        //     TestId = test.Id,
                        //     FilePath = filePath,
                        //     UploadedAt = DateTime.Now
                        // };
                        // _db.TestFiles.Add(testFile);
                    }
                }
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "Test created successfully", testId = test.Id });
        }

        [HttpPost("/addCustomer")]
        public async Task<IActionResult> AddCustomer([FromForm] CustomerCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var customer = new Customer
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                HashedPassword = request.Password
            };


            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();


            return Ok(new { message = "Test created successfully", customerId = customer.Id });
        }
        public class CustomerCreateRequest
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Email { get; set; }
            public string? Password { get; set; }

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

    public class CustomerStorage
    {
        public static Customer? Customer;
    }

    // Модель запроса для создания теста
    public class TestCreateRequest
    {
        [Required(ErrorMessage = "TestName is required")]
        public string? TestName { get; set; }

        [Required(ErrorMessage = "TestType is required")]
        public string? TestType { get; set; }

        public string? Description { get; set; } // Необязательное

        [Required(ErrorMessage = "Status is required")]
        public string? Status { get; set; }

        public string? Parameters { get; set; } // Необязательное, JSON-строка

        [Required(ErrorMessage = "Status is required")]
        public string? CustomerId { get; set; } // Необязательное, строка GUID

        public IFormFileCollection? Files { get; set; } // Необязательное
    }
}


