Magic - Magic Automatically Generates Incidental Code.

--- Set Up ---

Magic uses the Genuilder library (genuilder.codeplex.com) to hook into the 
MSBuild system and run code generation plugins before the compiler gets given
the source code.

To use, reference the following assemblies from your project:
Magic.Gen.dll
Genuilder.dll
Genuilder.Extensibility.dll
ICSharpCode.NRefactory.dll

Modify your project's .csproj file by adding the following Import after
<Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />

  <Import Project="..\..\lib\genuilder\alltargets.targets"/>

You may need to adjust the Project path to correctly point to 
alltargets.targets. This import allows Genuilder to hook into MSBuild.


--- Value Object Generator ---

Do you want to create an immutable Value Object (in the DDD sense of the term)?
Are you annoyed by all the C# code you have to write to do this properly?
You need a constructor to take all the values that make up the value. You need
the properties. You should overload Equals (and then also GetHashCode) to 
implement value-based comparison (instead of .NET's default object reference 
comparison). Finally providing operator overloads for == and != is a good idea
too... ugh a silly amount of code right?!

By using Magic you can simply write:

partial class Money : IValueObject {
    string currency;
    decimal amount;
}

Magic will generate the rest of the class for you; constructor, Equals, 
GetHashCode, == and !=. All you have to do is make the class partial and add 
the IValueObject marker interface.

Then you can continue using the class:

var m = new Money("GBP", 100m); // Use the constructor
Console.WriteLine(m.Currency); // Access the generated readonly property.
var ok = (m == new Money("GBP", 100m)); // Use the equality operator.
//etc


--- Dependency Constructor Generator ---

Following the dependency inversion principle usually means you'll be passing 
anything a class depends on in via its constructor. For example, in ASP.NET MVC
a constructor could depend on a repository and an email service.

namespace WebApp.Controllers {

public class HomeController : Controller {
    public HomeController(IRepository repository, IEmailService emailService) {
        this.repository = repository;
	this.emailService = emailService;
    }
    IRepository repository;
    IEmailService emailService;

    public ActionResult Index() {
    	var customers = repository.GetAllCustomers();
	emailService.SendEmail("admin@test.com", "got customers");
	return View(customers);
    }

}
}

Whilst tools like Resharper can automate the creation of this code, it's still 
code you have to read and maintain over time. It violates DRY - I count the word
"repository" 6 times there!

Using Magic we can simplify the code to this:

using Magic;
namespace WebApp.Controllers {

public partial class HomeController : Controller, 
    IDependOn<IRepository, IEmailService>
{
    public ActionResult Index() {
    	var customers = repository.GetAllCustomers();
	emailService.SendEmail("admin@test.com", "got customers");
	return View(customers);
    }
}
}

When we build the project now, a code generation plugin will run and see we have
declared a class with the IDependsOn interface. It generates another partial 
class for HomeController (note we had to add the partial modifier to our code).
This partial class contains a field for each dependency, named according to a 
simple camel-cased convention. The constructor is also generated. It looks just
like what we would have written by hand.

Often a constructor will take some data as well as dependencies.
The code generation will take existing constructors into account. So we can 
write code like this:

partial class LogSearcher : IDependOn<ILog> {
    private LogSearcher(string searchTerm) {
        this.searchTerm = searchTerm;
	log.Init();
    }
}

Magic will generate a public constructor that looks like something like this:

partial class LogSearcher {
    public LogSearcher(string searchTerm, ILog log) {
        this.log = log;
	this.searchTerm = searchTerm;
	log.Init();
    }
}

Magic clones the existing constructor body instead of calling it directly.
This is so you can use the dependencies in your constructor.

The IDependOn interface has up to 5 generic parameters. If your class needs more
than 5 dependencies maybe it's time to refactor that class?!

--- Coming Soon --

More code-generation plugins for Magic
