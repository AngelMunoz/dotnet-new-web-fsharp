namespace PlainWeb

open System
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http

[<ApiController; Route("[controller]")>]
type UploadsController(logger: ILogger<UploadsController>) =
    inherit ControllerBase()

    [<HttpPost("user-avatar");
      Consumes("multipart/form-data");
      ProducesResponseType(StatusCodes.Status204NoContent);
      ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member ctrl.UserAvatar(avatar: IFormFile) : Task<IActionResult> =
        task {
            logger.LogInformation("UserAvatar Got Called")

            let avatar = avatar |> Option.ofObj

            match avatar with
            | None -> return ctrl.BadRequest("The request must contain a file")
            | Some avatar ->

                if avatar.ContentType.StartsWith("image/", StringComparison.InvariantCultureIgnoreCase) then
                    // do what you want with the file
                    return ctrl.NoContent()
                else
                    return ctrl.BadRequest("The file must be an image")
        }

[<RequireQualifiedAccess>]
module MinimalHandlers =
    type UploadAvatar =
        interface
        end

    let private indexHandler () = "Hello World!"

    let private uploadAvatar (context: HttpContext) (logger: ILogger<UploadAvatar>) =
        task {
            logger.LogInformation "uploadAvatar Got Called"
            let! form = context.Request.ReadFormAsync(context.RequestAborted)
            let userAvatar = form.Files.GetFile "user-avatar" |> Option.ofObj

            match userAvatar with
            | Some file ->
                if file.ContentType.StartsWith("image/", StringComparison.InvariantCultureIgnoreCase) then
                    // do what you want with the file
                    return Results.NoContent()
                else
                    return Results.BadRequest("The file must be an image")
            | None -> return Results.BadRequest("The request must contain a file")
        }

    let register (app: WebApplication) =
        app
            .MapGet("/", Func<string>(indexHandler))
            .Produces(StatusCodes.Status200OK, "text/plain")
        |> ignore

        app
            .MapPost("/uploads", Func<HttpContext, ILogger<UploadAvatar>, Task<IResult>>(uploadAvatar))
            .Accepts("multipart/form-data")
            .Produces(StatusCodes.Status204NoContent, "text/plain")
            .ProducesProblem(StatusCodes.Status400BadRequest, "text/plain")
        |> ignore

module Program =
    [<EntryPoint>]
    let main args =
        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddControllers() |> ignore

        let app = builder.Build()

        MinimalHandlers.register app
        // ProductHandlers.register app
        // ProductHandlers.register app

        app.MapControllers() |> ignore

        app.Run()

        0 // Exit code
