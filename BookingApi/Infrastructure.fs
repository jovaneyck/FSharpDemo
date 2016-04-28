module BookingApi.Infrastructure

open System.Web.Http

type HttpRouteDefaults = { Controller : string; Id : obj }

let ConfigureRoutes (configuration : HttpConfiguration) =
    configuration.Routes.MapHttpRoute(
            "DefaultAPI",
            "{controller}/{id}",
            { Controller = "Home"; Id = RouteParameter.Optional }) 
    |> ignore

let ConfigureFormatting (configuration : HttpConfiguration) =
    configuration.Formatters.JsonFormatter.SerializerSettings.ContractResolver
        <- Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()

let Configure c = 
    ConfigureRoutes c
    ConfigureFormatting c