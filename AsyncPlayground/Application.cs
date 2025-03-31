using AsyncPlayground.Entities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AsyncPlayground
{
    internal class Application
    {
        private Dictionary<string, int> _cache = new() { ["x"] = 42 };
        private readonly ApplicationContext _context;

        public Application(ApplicationContext context)
        {
            _context = context;
        }

        public async Task Go()
        {
            Task1_HelloWorld();
            await Task2_ReturnTaskToAddEmployeeAsync();
            await Task3_Exception();
            Console.WriteLine($"Number of Employees: {(await Task4_ReturnEmployees()).Count}");
            await Task5_Disposal();
            Console.WriteLine($"Task6_MultipleCalls: {(await Task6_MultipleCalls()).Count}");
            Console.WriteLine($"Task7_1_GetFirstEmployeeName: {await Task7_1_GetFirstEmployeeName()}");
            await Task7_2_CorrectBlocking();
            await Task8_FireAndForget();
            await Task9_GetCachedValueAsync();

            var cts = new CancellationTokenSource();
            cts.Cancel();
            await Task10_Cancellation(cts.Token);
        }

        // Task 1. Unnecessary State Machine involved.
        private void Task1_HelloWorld()
        {
            Console.WriteLine("Hello World!");
        }

        // Task 2. Everything looks fine... or does it?
        private Task<int> Task2_ReturnTaskToAddEmployeeAsync()
        {
            try
            {
                return CreateEmployee();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private async Task<int> CreateEmployee()
        {
            // Add a new employee to the database
            var employee = new Employee
            {
                Name = "John Doe",
                Department = "IT"
            };
            _context.Employees.Add(employee);
            var result = await _context.SaveChangesAsync();
            return result;
        }

        // Task 3. We should see the exception message in the output
        private async Task Task3_Exception()
        {
            await CatchTheException();
        }

        private async Task AsyncVoidMethodThrowsException()
        {
            await Task.Delay(100);
            throw new Exception("Hmmm, something went wrong!");
        }

        public async Task CatchTheException()
        {
            try
            {
                await AsyncVoidMethodThrowsException();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        // Task 4. Return the list of employees
        private async Task<List<Employee>> Task4_ReturnEmployees()
        {
            return await ReturnEmployeesListAsync();
        }

        private async Task<List<Employee>> ReturnEmployeesListAsync()
        {
            return await _context.Employees.ToListAsync();
        }

        // Task 5. Avoid early disposal
        private async Task Task5_Disposal()
        {
            var result = await ReturnTaskToReadFileEarlyDisposeAsync();
            Console.WriteLine("Task5 " + result);
        }

        public async Task<string> ReturnTaskToReadFileEarlyDisposeAsync()
        {
            using (var reader = new StreamReader("config.json"))
            {
                return await reader.ReadToEndAsync();
            }
        }

        // Task 6. Optimize multiple calls that are not dependent on each other
        private async Task<List<Employee>> Task6_MultipleCalls()
        {
            var employeesFromDepartment1 = GetEmployeesFromDepartmentAsync("IT");
            var employeesFromDepartment2 = GetEmployeesFromDepartmentAsync("Financial");
            var employeesFromDepartment3 = GetEmployeesFromDepartmentAsync("BI");

            await Task.WhenAll(new Task[] { employeesFromDepartment1, employeesFromDepartment2, employeesFromDepartment3 });

            var result = new List<Employee>();
            result.AddRange(employeesFromDepartment1.Result.Concat(employeesFromDepartment2.Result).Concat(employeesFromDepartment3.Result));

            return result;
        }

        private async Task<List<Employee>> GetEmployeesFromDepartmentAsync(string department)
        {
            return await _context.Employees.Where(e => e.Department == department).ToListAsync();
        }

        // Task 7.1. I just don't like AggregateExceptions
        private Task<string?> Task7_1_GetFirstEmployeeName()
        {
            return GetFirstEmployeeNameAsync();
        }

        private async Task<string?> GetFirstEmployeeNameAsync()
        {
            var employee = await _context.Employees.FirstOrDefaultAsync();
            return employee?.Name;
        }

        // Task 7.2. I just don't like AggregateExceptions
        private async Task Task7_2_CorrectBlocking()
        {
            await LongImportantJobThatShouldBeAwaited();
        }

        private async Task LongImportantJobThatShouldBeAwaited()
        {
            await Task.Delay(5000);
        }

        // Task 8. Avoid Fire-and-Forget Without Logging or Handling
        private async Task Task8_FireAndForget()
        {
            try
            {
                await DoBackgroundWorkAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Task8_FireAndForget: {ex.Message}");
            }
        }

        private async Task DoBackgroundWorkAsync()
        {
            await Task.Delay(1000);
            Console.WriteLine("Background work completed!");
            throw new Exception("Background work failed!");
        }

        // Task 9: This method is being called really often. Try to optimize its memory consumption.
        public async Task<int> Task9_GetCachedValueAsync()
        {
            //What do you mean it's called often? It's not clear task, next time provide more info.

            if (_cache.TryGetValue("x", out var value))
                return value;

            return await FetchValueAsync();
        }

        private async Task<int> FetchValueAsync()
        {
            await Task.Delay(100);
            return new Random().Next();
        }

        // Task 10. Provide cancellation mechanism for the long-running task
        private async Task Task10_Cancellation(CancellationToken token)
        {
            await LongRunningTaskAsync(token);
        }
        
        public async Task LongRunningTaskAsync(CancellationToken token)
        {
            for (int i = 0; i < 100; i++)
            {
                //Simulate an async call that takes some time to complete
                await Task.Delay(1000);
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("LongRunningTaskAsync: cancellation requested");
                    break;
                }
            }
        }
    }
}
