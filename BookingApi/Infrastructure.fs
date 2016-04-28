module BookingApi.Infrastructure

open System.Web.Http

type HttpRouteDefaults = { Controller : string; Id : obj }

let ConfigureRoutes (configuration : HttpConfiguration) =
    configuration.Routes.MapHttpRoute(
            "DefaultAPI",
            "{controller}/{id}",
            { Controller = "Home"; Id = RouteParameter.Optional }) 
        |> ignore

let Configure = ConfigureRoutes