using System;
using Structurizr.Analysis;
using System.Linq;
using Structurizr.Api;

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
            Model model = workspace.Model;
            ViewSet views = workspace.Views;
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
            webApplication.Uses(database, "Reads from and writes to");

            ComponentFinder componentFinder = new ComponentFinder(
                webApplication,
                typeof(ContosoUniversity.MvcApplication).Namespace, // doing this typeof forces the ContosoUniversity assembly to be loaded
                new TypeMatcherComponentFinderStrategy(
                    new InterfaceImplementationTypeMatcher(typeof(System.Web.Mvc.IController), null, "ASP.NET MVC Controller"),
                    new ExtendsClassTypeMatcher(typeof(System.Data.Entity.DbContext), null, "Entity Framework DbContext")
                )
                //new TypeSummaryComponentFinderStrategy(@"C:\Users\simon\ContosoUniversity\ContosoUniversity.sln", "ContosoUniversity")
            );
            componentFinder.FindComponents();

            // connect the user to the web MVC controllers
            webApplication.Components.ToList().FindAll(c => c.Technology == "ASP.NET MVC Controller").ForEach(c => universityStaff.Uses(c, "uses"));

            // connect all DbContext components to the database
            webApplication.Components.ToList().FindAll(c => c.Technology == "Entity Framework DbContext").ForEach(c => c.Uses(database, "Reads from and writes to"));

            // link the components to the source code
            foreach (Component component in webApplication.Components)
            {
                foreach (CodeElement codeElement in component.CodeElements)
                {
                    if (codeElement.Url != null)
                    {
                        codeElement.Url = codeElement.Url.Replace(new Uri(@"C:\Users\simon\ContosoUniversity\").AbsoluteUri, "https://github.com/simonbrowndotje/ContosoUniversity/blob/master/");
                        codeElement.Url = codeElement.Url.Replace('\\', '/');
                    }
                }
            }

            // rather than creating a component model for the database, let's simply link to the DDL
            // (this is really just an example of linking an arbitrary element in the model to an external resource)
            database.Url = "https://github.com/simonbrowndotje/ContosoUniversity/tree/master/ContosoUniversity/Migrations";

            SystemContextView contextView = views.CreateSystemContextView(contosoUniversity, "Context", "The system context view for the Contoso University system.");
            contextView.AddAllElements();

            ContainerView containerView = views.CreateContainerView(contosoUniversity, "Containers", "The containers that make up the Contoso University system.");
            containerView.AddAllElements();

            ComponentView componentView = views.CreateComponentView(webApplication, "Components", "The components inside the Contoso University web application.");
            componentView.AddAllElements();

            // create an example dynamic view for a feature
            DynamicView dynamicView = views.CreateDynamicView(webApplication, "GetCoursesForDepartment", "A summary of the \"get courses for department\" feature.");
            Component courseController = webApplication.GetComponentWithName("CourseController");
            Component schoolContext = webApplication.GetComponentWithName("SchoolContext");
            dynamicView.Add(universityStaff, "Requests the list of courses from", courseController);
            dynamicView.Add(courseController, "Uses", schoolContext);
            dynamicView.Add(schoolContext, "Gets a list of courses from", database);

            // add some styling
            styles.Add(new ElementStyle(Tags.Person) { Background = "#0d4d4d", Color = "#ffffff", Shape = Shape.Person });
            styles.Add(new ElementStyle(Tags.SoftwareSystem) { Background = "#003333", Color = "#ffffff" });
            styles.Add(new ElementStyle(Tags.Container) { Background = "#226666", Color = "#ffffff" });
            styles.Add(new ElementStyle("Database") { Shape = Shape.Cylinder });
            styles.Add(new ElementStyle(Tags.Component) { Background = "#407f7f", Color = "#ffffff" });

            StructurizrClient structurizrClient = new StructurizrClient("key", "secret");
//            structurizrClient.MergeWorkspace(5651, workspace);
        }
    }
}

