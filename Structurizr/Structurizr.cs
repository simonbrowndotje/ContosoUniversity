using Structurizr.Analysis;
using Structurizr.Client;
using Structurizr.Model;
using Structurizr.View;
using System.Linq;

namespace Structurizr
{

    /// <summary>
    /// This is a program that creates a software architecture model for the sample "Contoso University" application.
    ///  - Source code: available at https://code.msdn.microsoft.com/ASPNET-MVC-Application-b01a9fe8
    ///  - Tutorial: https://www.asp.net/mvc/overview/getting-started/getting-started-with-ef-using-mvc/creating-an-entity-framework-data-model-for-an-asp-net-mvc-application
    /// </summary>
    class Structurizr
    {
        static void Main(string[] args)
        {
            Workspace workspace = new Workspace("Contoso University", "A software architecture model of the Contoso University sample project.");
            Model.Model model = workspace.Model;
            ViewSet views = workspace.Views;
            views.Configuration.Metadata = "Top";
            Styles styles = views.Configuration.Styles;

            Person universityStaff = model.AddPerson("University Staff", "A staff member of the Contoso University.");
            SoftwareSystem contosoUniversity = model.AddSoftwareSystem("Contoso University", "Allows staff to view and update student, course, and instructor information.");
            universityStaff.Uses(contosoUniversity, "uses");

            // if the client-side of this application was richer (e.g. it was a single-page app), I would include the web browser
            // as a container (i.e. User --uses-> Web Browser --uses-> Web Application (backend for frontend) --uses-> Database)
            Container webApplication = contosoUniversity.AddContainer("Web Application", "Allows staff to view and update student, course, and instructor information.", "Microsoft ASP.NET MVC");
            Container database = contosoUniversity.AddContainer("Database", "Stores information about students, courses and instructors", "Microsoft SQL Server Express LocalDB");
            database.AddTags("Database");
            universityStaff.Uses(webApplication, "Uses", "HTTPS");

            ComponentFinder componentFinder = new ComponentFinder(
                webApplication,
                typeof(ContosoUniversity.MvcApplication).Namespace, // doing this typeof forces the ContosoUniversity assembly to be loaded
                new AssemblyScanningComponentFinderStrategy(
                    new InterfaceImplementationTypeMatcher(typeof(System.Web.Mvc.IController), null, "ASP.NET MVC Controller"),
                    new ExtendsClassTypeMatcher(typeof(System.Data.Entity.DbContext), null, "Entity Framework DbContext")
                ),
                new TypeSummaryComponentFinderStrategy(@"C:\Users\simon\ContosoUniversity\ContosoUniversity.sln", "ContosoUniversity")
            );
            componentFinder.FindComponents();

            // wire up the user to the web MVC controllers
            webApplication.Components.ToList().FindAll(c => c.Technology == "ASP.NET MVC Controller").ForEach(c => universityStaff.Uses(c, "uses"));

            // and all DbContext components to the database
            webApplication.Components.ToList().FindAll(c => c.Technology == "Entity Framework DbContext").ForEach(c => c.Uses(database, "Reads from and writes to"));

            // link the components to the source code
            foreach (Component component in webApplication.Components)
            {
                if (component.SourcePath != null)
                {
                    component.SourcePath = component.SourcePath.Replace(@"C:\Users\simon\ContosoUniversity\", "https://github.com/simonbrowndotje/ContosoUniversity/blob/master/");
                    component.SourcePath = component.SourcePath.Replace('\\', '/');
                }
            }

            SystemContextView contextView = views.CreateContextView(contosoUniversity);
            contextView.AddAllElements();

            ContainerView containerView = views.CreateContainerView(contosoUniversity);
            containerView.AddAllElements();

            ComponentView componentView = views.CreateComponentView(webApplication);
            componentView.AddAllElements();

            // add some styling
            styles.Add(new ElementStyle(Tags.Person) { Background = "#0d4d4d", Color = "#ffffff", Shape = Shape.Person });
            styles.Add(new ElementStyle(Tags.SoftwareSystem) { Background = "#003333", Color = "#ffffff" });
            styles.Add(new ElementStyle(Tags.Container) { Background = "#226666", Color = "#ffffff" });
            styles.Add(new ElementStyle("Database") { Shape = Shape.Cylinder });
            styles.Add(new ElementStyle(Tags.Component) { Background = "#407f7f", Color = "#ffffff" });

            StructurizrClient structurizrClient = new StructurizrClient("key", "secret");
            structurizrClient.MergeWorkspace(9581, workspace);

            System.Console.ReadKey();
        }
    }
}
