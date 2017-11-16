# PullMarketing
the FastStats Pull Marketing Module is a system for providing bulk upload and fast access to FastStats data by "Pull" sources such as websites, apps, etc.

The module consists of:
* A RESTful API with a [Swagger](http://swagger.io/) spec.
* A console application to perform bulk uploads from the command line (useful for testing).
* Backend connections for AWS [DynamoDB](https://aws.amazon.com/dynamodb/) and [MongoDB](https://www.mongodb.com/) (currently).

The application is built upon [.Net Core](https://www.microsoft.com/net/core) and [.Net Standard](https://github.com/dotnet/standard)
and the API uses [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) to provide the Swagger specification.

Currently the Pull Marketing Module is not a fully supported release, but you are free to evaluate it.

## Getting Started with the API

At the moment there is no binary installation, so you have to build up the files from source.

For installation under Windows:

1. Install the current .Net Core runtime and Windows Server Hosting components (from [here](https://www.microsoft.com/net/download/core#/runtime)).
2. Download/Clone the repository and build the API using Visual Studio 2017 or the .Net Core CLI.
3. Build and publish the API using the .Net Core CLI

    ~~~~
    dotnet publish --configuration Release
    ~~~~

4. Copy the files (built into Apteco.PullMarketing.Api\bin\Release\publish) into IIS (i.e. C:\inetpub\wwwroot\PullMarketing)
5. In IIS Manager, create a new App Pool and set the .Net CLR version to to "No Managed Code".
6. Then in IIS Manager, go to the directory you copied the files into and mark it as an application, using the App Pool you've just created.
7. Edit the appsettings.json file to configure the connection to your backend data store.

You should then be able to browse to the URL http://yourserver/PullMarketing/swagger to see and experiment with the API specification

## Integrating with FastStats

At the moment there are only experimental ways for calling the Pull Marketing module with FastStats PeopleStage.  Watch this space for a more integrated solution.

## Getting Started with the console application

To run the console application, just download/clone the repository and build the application (as in step 2 above).  Then either:

* Go to the root of the application and type:

~~~~
dotnet run -p src\Apteco.PullMarketing.Console\Apteco.PullMarketing.Console.csproj
~~~~

* Build up the project and then run it from the bin directory

~~~~
dotnet build src\Apteco.PullMarketing.Console --configuration Release
cd src\Apteco.PullMarketing.Console\bin\Release\netcoreapp1.1
dotnet Apteco.PullMarketing.Console.dll
~~~~

The console application projects a brief help message and will also generate example specification files needed to perform bulk uploads.
