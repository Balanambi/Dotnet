using AutoMapper;
using Newtonsoft.Json;
using Serilog;

namespace DependencyAnalysisPOC
{
    // Simple model for demonstration
    public class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public class PersonDto
    {
        public string FullName { get; set; } = string.Empty;
        public int Years { get; set; }
    }

    // AutoMapper profile
    public class PersonMappingProfile : Profile
    {
        public PersonMappingProfile()
        {
            CreateMap<Person, PersonDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Years, opt => opt.MapFrom(src => src.Age));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Dependency Analysis POC ===");

            // Configure Serilog (only basic console logging)
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                // Use AutoMapper for object mapping
                var config = new MapperConfiguration(cfg => cfg.AddProfile<PersonMappingProfile>());
                var mapper = config.CreateMapper();

                var person = new Person { Name = "John Doe", Age = 30 };
                var personDto = mapper.Map<PersonDto>(person);

                Log.Information("Person mapped: {FullName}, Age: {Years}", personDto.FullName, personDto.Years);

                // Use Newtonsoft.Json for serialization
                var jsonString = JsonConvert.SerializeObject(personDto, Formatting.Indented);
                Log.Information("JSON Output:\n{Json}", jsonString);

                // Deserialize back
                var deserializedPerson = JsonConvert.DeserializeObject<PersonDto>(jsonString);
                Log.Information("Deserialized: {FullName}", deserializedPerson?.FullName);

                Console.WriteLine("\nPOC completed successfully!");
                Console.WriteLine("This application uses:");
                Console.WriteLine("- AutoMapper (for object mapping)");
                Console.WriteLine("- Newtonsoft.Json (for JSON serialization)");
                Console.WriteLine("- Serilog with Console sink (for logging)");
                Console.WriteLine("\nNOTE: Many other libraries are referenced but NOT used:");
                Console.WriteLine("- EntityFramework Core & SQL Server provider");
                Console.WriteLine("- Serilog File sink");
                Console.WriteLine("- FluentValidation");
                Console.WriteLine("- Microsoft.Extensions.Configuration packages");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
