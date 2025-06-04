using ElstromVPKS.DbRequest;
using ElstromVPKS.JWT;
using ElstromVPKS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Spire.Doc;
using System;
using System.Collections.Generic;
using System.IO;
using HarfBuzzSharp;
using BCrypt.Net;
using System.Text.Json.Serialization;
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
        private readonly string _documentsPath;
        private readonly string _templatesPath;

        public UserController(ElstromContext appDBContext, JwtProvider jwtProvider)
        {
            _db = appDBContext;
            _jwtProvider = jwtProvider;
            _userRequests = new UserRequests(_db);
            _documentsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Documents");
            _templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates");
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
        [Authorize(Roles = "Отдел, Глава инженерного отдела")]
        [Route("/getEmployees")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
        {
            return await _db.Employees.Where(e => e.DeletedAt == null).ToListAsync();
        }

        [HttpGet]
        [Authorize(Roles = "Отдел, Глава инженерного отдела,Инженер")]
        [Route("/getCustomers")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            return await _db.Customers.Where(e => e.DeletedAt == null).ToListAsync();
        }

        [HttpPost]
        [Authorize(Roles = "Отдел, Глава инженерного отдела, Инженер")]
        [Route("/addEmployee")]
        public async Task<ActionResult<Employee>> CreateEmployee([FromBody] Employee employee)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                    .ToList();
                Console.WriteLine("Ошибки валидации модели:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"{error.Key}: {string.Join(", ", error.Errors)}");
                }
                return BadRequest(ModelState);
            }

            try
            {
                // Проверка уникальности username
                if (await _db.Employees.AnyAsync(e => e.Username == employee.Username))
                {
                    ModelState.AddModelError("username", "Имя пользователя уже существует.");
                    return BadRequest(ModelState);
                }

                // Генерация GUID и установка текущей даты
                employee.Id = Guid.NewGuid();
                employee.CreatedAt = DateTime.UtcNow;

                // Хеширование пароля
                var salt = BCrypt.Net.BCrypt.GenerateSalt();
                employee.HashedPassword = BCrypt.Net.BCrypt.HashPassword(employee.HashedPassword, salt);
                employee.Salt = salt;


                _db.Employees.Add(employee);
                await _db.SaveChangesAsync();

                // Возвращаем объект без чувствительных данных
                var responseEmployee = new Employee
                {
                    Id = employee.Id,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    Username = employee.Username,
                    Role = employee.Role,
                    CreatedAt = employee.CreatedAt,
                    Salt = employee.Salt
                };

                return CreatedAtAction(nameof(GetEmployeeById), new { id = employee.Id }, responseEmployee);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при создании сотрудника: {ex.Message}");
            }
        }

        // ... (другие методы остаются без изменений)


        [HttpPost]
        [Authorize(Roles = "Отдел, Глава инженерного отдела, Инженер")]
        [Route("/addCustomer")]
        public async Task<ActionResult<Customer>> CreateCustomer([FromBody] Customer customer)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                    .ToList();
                Console.WriteLine("Ошибки валидации модели:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"{error.Key}: {string.Join(", ", error.Errors)}");
                }
                return BadRequest(ModelState);
            }

            try
            {
                // Проверка уникальности email
                if (await _db.Customers.AnyAsync(c => c.Email == customer.Email))
                {
                    ModelState.AddModelError("email", "Email уже зарегистрирован.");
                    return BadRequest(ModelState);
                }

                // Генерация GUID и установка текущей даты
                customer.Id = Guid.NewGuid();
                customer.CreatedAt = DateTime.UtcNow;

                // Хеширование пароля
                var salt = BCrypt.Net.BCrypt.GenerateSalt();
                customer.HashedPassword = BCrypt.Net.BCrypt.HashPassword(customer.HashedPassword, salt);
                customer.Salt = salt;


                _db.Customers.Add(customer);
                await _db.SaveChangesAsync();

                // Возвращаем объект без чувствительных данных
                var responseCustomer = new Customer
                {
                    Id = customer.Id,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Email = customer.Email,
                    CreatedAt = customer.CreatedAt,
                    Salt = customer.Salt
                };

                return CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id }, responseCustomer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при создании заказчика: {ex.Message}");
            }
        }


        [HttpGet]
        [Authorize(Roles = "Отдел, Глава инженерного отдела, Инженер")]
        [Route("/getCustomerById/{id}")]
        public async Task<ActionResult<Customer>> GetCustomerById(Guid id)
        {
            var customer = await _db.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound("Заказчик с данным ID не найден");
            }

            // Возвращаем объект без чувствительных данных
            var responseCustomer = new Customer
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                CreatedAt = customer.CreatedAt
            };

            return responseCustomer;
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
        [Authorize(Roles = "Отдел, Глава инженерного отдела,Инженер")]
        [Route("/getTests")]
        public async Task<ActionResult<IEnumerable<Test>>> GetTests()
        {

            return await _db.Tests.ToListAsync();
        }

        //[HttpGet]
        //[Authorize] // Только для аутентифицированных пользователей
        //[Route("/getCustomerTests")]
        //public async Task<ActionResult<IEnumerable<object>>> GetCustomerTests()
        //{
        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        //    {
        //        return Unauthorized("Невалидный пользователь");
        //    }

        //    //// Проверяем, что пользователь — клиент
        //    //var customerExists = await _db.Customers.AnyAsync(c => c.Id == userId);
        //    //if (!customerExists)
        //    //{
        //    //    return Forbid("Пользователь не является клиентом");
        //    //}

        //    // Получаем только завершённые тесты, назначенные клиенту
        //    var tests = await _db.TestCustomerAssignments
        //        .Where(tca => tca.CustomerId == userId && tca.Test.Status == "Completed")
        //        .Select(tca => new
        //        {
        //            Id = tca.Test.Id,
        //            TestName = tca.Test.TestName,
        //            TestType = tca.Test.TestType,
        //            Status = tca.Test.Status,
        //            //Description = tca.Test.Description,
        //            CreatedAt = tca.Test.CreatedAt,
        //            Parametrs = tca.Test.Parametrs,
        //            AssignedAt = tca.AssignedAt
        //        })
        //        .ToListAsync();

        //    return Ok(tests);
        //}

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
        [Authorize(Roles = "Инженер")]
        public async Task<IActionResult> AddTest([FromForm] TestCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToList();
                Console.WriteLine("Ошибки валидации модели:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"{error.Key}: {string.Join(", ", error.Errors.Select(e => e.ErrorMessage))}");
                }
                return BadRequest(new { message = "Ошибка валидации", errors });
            }

            var employeeId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var test = new Test
            {
                Id = Guid.NewGuid(),
                TestName = request.TestName,
                TestType = request.TestType,
                Description = request.Description,
                Status = request.Status,
                Parametrs = request.Parameters ?? "[]",
                CreatedAt = DateTime.Now,
                //CreatedBy = employeeId
            };

            await _db.Tests.AddAsync(test);
            await _db.SaveChangesAsync();

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
                await _db.TestCustomerAssignments.AddAsync(assignment);
            }

            // Генерация PDF документа
            if (!string.IsNullOrEmpty(request.TemplateType))
            {
                var templateInfo = GetTemplateInfo(request.TemplateType);
                if (templateInfo == null)
                {
                    return BadRequest("Недопустимый тип шаблона");
                }

                try
                {
                    if (string.IsNullOrEmpty(request.Parameters))
                    {
                        return BadRequest("Параметры шаблона отсутствуют");
                    }

                    Console.WriteLine($"Полученные параметры: {request.Parameters}");
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // Игнорировать регистр при десериализации
                    };
                    var parameters = JsonSerializer.Deserialize<List<Parameter>>(request.Parameters, jsonOptions);
                    if (parameters == null || !parameters.Any())
                    {
                        return BadRequest("Параметры шаблона пусты или некорректны");
                    }

                    // Проверка на пустые или null ключи
                    var invalidParams = parameters
                        .Select((p, index) => new { Param = p, Index = index })
                        .Where(p => string.IsNullOrEmpty(p.Param.Key))
                        .ToList();
                    if (invalidParams.Any())
                    {
                        var invalidIndices = string.Join(", ", invalidParams.Select(p => p.Index));
                        Console.WriteLine($"Обнаружены параметры с пустыми ключами на позициях: {invalidIndices}");
                        return BadRequest($"Обнаружены параметры с пустыми ключами на позициях: {invalidIndices}");
                    }

                    // Проверка на дубликаты ключей
                    var duplicates = parameters
                        .GroupBy(p => p.Key)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();
                    if (duplicates.Any())
                    {
                        Console.WriteLine($"Дубликаты ключей: {string.Join(", ", duplicates)}");
                        return BadRequest($"Обнаружены дубликаты ключей в параметрах: {string.Join(", ", duplicates)}");
                    }

                    // Логирование параметров
                    Console.WriteLine("Параметры после десериализации:");
                    foreach (var param in parameters)
                    {
                        Console.WriteLine($"Key: '{param.Key}', Value: '{param.Value}'");
                    }

                    var replacements = parameters.ToDictionary(
                        p => p.Key,
                        p => p.Value ?? ""
                    );

                    var inputFilePath = Path.Combine(_templatesPath, templateInfo.TemplatePath);
                    var outputDir = Path.Combine(_documentsPath, test.Id.ToString());
                    var outputFileName = $"document_{test.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                    var outputFilePath = Path.Combine(outputDir, outputFileName);

                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    if (ReplaceTags(inputFilePath, outputFilePath, replacements))
                    {
                        var document = new Models.Document
                        {
                            Id = Guid.NewGuid(),
                            DocumentName = outputFileName,
                            OwnerId = test.Id,
                            FilePath = $"/Documents/{test.Id}/{outputFileName}",
                            Status = "Approved",
                            Version = 1,
                            CreatedAt = DateTime.Now
                        };
                        await _db.Documents.AddAsync(document);
                    }
                    else
                    {
                        Console.WriteLine($"Не удалось сгенерировать документ для теста {test.Id}: шаблон {inputFilePath}");
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Ошибка десериализации параметров: {ex.Message}. Параметры: {request.Parameters}");
                    return BadRequest("Ошибка десериализации параметров шаблона");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при создании документа: {ex.Message}. Параметры: {request.Parameters}");
                    return StatusCode(500, "Ошибка при создании документа");
                }
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "Тест успешно создан", testId = test.Id });
        }

        private static bool ReplaceTags(string inputFilePath, string outputFilePath, Dictionary<string, string> replacements)
        {
            try
            {
                if (!System.IO.File.Exists(inputFilePath))
                {
                    Console.WriteLine("Шаблон не найден: " + inputFilePath);
                    return false;
                }

                Spire.Doc.Document document = new Spire.Doc.Document();
                document.LoadFromFile(inputFilePath);

                foreach (var replacement in replacements)
                {
                    document.Replace(replacement.Key, replacement.Value, true, true);
                }

                document.SaveToFile(outputFilePath, FileFormat.PDF);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при генерации документа: " + ex.Message);
                return false;
            }
        }

        private static TemplateInfo GetTemplateInfo(string templateType)
        {
            var templates = new Dictionary<string, TemplateInfo>
            {
                ["Receipt"] = new TemplateInfo { TemplatePath = "Receipt/Example.docx" },
                ["Certificate"] = new TemplateInfo { TemplatePath = "Certificate/CertTemplate.docx" }
            };
            return templates.ContainsKey(templateType) ? templates[templateType] : null;
        }

        // Обновлённая модель TestCreateRequest
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

            public string? CustomerId { get; set; } // Необязательное, строка GUID

            [Required(ErrorMessage = "TemplateType is required")]
            public string? TemplateType { get; set; } // Добавлено для выбора шаблона

            public IFormFileCollection? Files { get; set; } // Необязательное
        }

        public class Parameter
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }

        public class TemplateInfo
        {
            public string TemplatePath { get; set; }
        }


        [HttpGet("/getCustomerTests")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> GetCustomerTests()
        {
            try
            {
                // Получаем authTokenCustomer из куки
                var token = Request.Cookies["authTokenCustomer"];
                Console.WriteLine($"authTokenCustomer: {token}");

                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("authTokenCustomer cookie is missing");
                    return Unauthorized("Токен клиента не найден");
                }

                // Валидируем токен и извлекаем claims
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var userIdClaim = jwtToken.Claims.FirstOrDefault().Value;
                Console.WriteLine($"UserIdClaim from authTokenCustomer: {userIdClaim}");

                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    Console.WriteLine("Invalid user: userIdClaim is empty or not a GUID");
                    return Unauthorized("Невалидный пользователь");
                }

                // Проверяем, что пользователь — клиент
                var customerExists = await _db.Customers.AnyAsync(c => c.Id == userId);
                if (!customerExists)
                {
                    Console.WriteLine($"User with ID {userId} is not a customer");
                    return StatusCode(403, "Пользователь не является клиентом");
                }

                // Получаем завершённые тесты клиента
                var tests = await _db.TestCustomerAssignments
                    .Where(tca => tca.CustomerId == userId && tca.Test.Status == "Completed")
                    .Select(tca => new
                    {
                        Id = tca.Test.Id,
                        TestName = tca.Test.TestName,
                        TestType = tca.Test.TestType,
                        Status = tca.Test.Status,
                        Description = tca.Test.Description,
                        CreatedAt = tca.Test.CreatedAt,
                        Parametrs = tca.Test.Parametrs,
                        AssignedAt = tca.AssignedAt,
                        Documents = tca.Test.Documents.Select(d => new
                        {
                            d.Id,
                            d.DocumentName,
                            d.FilePath
                        }).ToList()
                    })
                    .ToListAsync();

                Console.WriteLine($"Found tests: {tests.Count}");
                return Ok(tests);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCustomerTests: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, "Произошла ошибка при получении тестов клиента");
            }
        }



        [HttpPut]
        [Authorize(Roles = "Отдел, Глава инженерного отдела")]
        [Route("/updateTestStatus/{id}")]
        public async Task<IActionResult> UpdateTestStatus(Guid id, [FromBody] UpdateTestStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var test = await _db.Tests.FindAsync(id);
            if (test == null)
            {
                return NotFound("Тест не найден");
            }

            // Проверяем, что новый статус валиден
            var validStatuses = new[] { "Planned", "In Progress", "Completed", "Cancelled" };
            if (!validStatuses.Contains(request.Status))
            {
                return BadRequest("Недопустимый статус теста");
            }

            test.Status = request.Status;
            test.UpdatedAt = DateTime.Now;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_db.Tests.Any(t => t.Id == id))
                {
                    return NotFound("Тест не найден");
                }
                throw;
            }

            return Ok(new { message = "Статус теста обновлён", testId = test.Id });
        }

        public class UpdateTestStatusRequest
        {
            public string Status { get; set; }
        }


        //[HttpPost("/addCustomer")]
        //public async Task<IActionResult> AddCustomer([FromForm] CustomerCreateRequest request)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    var customer = new Customer
        //    {
        //        FirstName = request.FirstName,
        //        LastName = request.LastName,
        //        Email = request.Email,
        //        HashedPassword = request.Password
        //    };


        //    _db.Customers.Add(customer);
        //    await _db.SaveChangesAsync();


        //    return Ok(new { message = "Test created successfully", customerId = customer.Id });
        //}
        public class CustomerCreateRequest
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Email { get; set; }
            public string? Password { get; set; }

        }

        // Получение деталей теста по его ID
        //[HttpGet("/getTestById/{id}")]
        //public async Task<IActionResult> GetTestById(Guid id)
        //{
        //    var test = await _db.Tests.FindAsync(id);
        //    if (test == null)
        //    {
        //        return NotFound(new { message = "Тест не найден" });
        //    }
        //    return Ok(test);
        //}

        [HttpGet("/getDocuments")]
        public IActionResult GetDocument(string filePath)
        {
            Console.WriteLine($"GetDocument вызван. Исходный filePath: {filePath}");

            // Декодируем путь
            var decodedFilePath = Uri.UnescapeDataString(filePath);
            Console.WriteLine($"Декодированный filePath: {decodedFilePath}");

            // Формируем полный путь, сохраняя структуру папок
            var filePathResolved = Path.Combine(_documentsPath, decodedFilePath);
            Console.WriteLine($"Полный путь к файлу: {filePathResolved}");

            if (!System.IO.File.Exists(filePathResolved))
            {
                Console.WriteLine($"Файл не найден: {filePathResolved}");
                return NotFound(new { message = "Документ не найден", path = filePathResolved });
            }

            var fileStream = new FileStream(filePathResolved, FileMode.Open, FileAccess.Read);
            return File(fileStream, "application/pdf", Path.GetFileName(filePathResolved));
        }

        [HttpGet("/getTestById/{id}")]
        public async Task<IActionResult> GetTestById(Guid id)
        {
            var test = await _db.Tests
                .Include(t => t.Documents) // Включаем связанные документы
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test == null)
            {
                return NotFound(new { message = "Тест не найден" });
            }

            // Формируем DTO для ответа
            var testDto = new
            {
                test.Id,
                test.TestName,
                test.TestType,
                test.Description,
                test.Status,
                test.CreatedAt,
                Documents = test.Documents.Select(d => new
                {
                    d.Id,
                    d.DocumentName,
                    d.FilePath,
                    d.Status,
                    d.CreatedAt
                }).ToList()
            };

            return Ok(testDto);
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


