module BookingApi.Infrastructure

open System
open System.Net.Http
open System.Web.Http
open System.Web.Http.Dispatcher
open System.Web.Http.Controllers
open BookingApi.HttpApi
open BookingApi.Messages
open BookingApi.Domain.Reservations

type Agent<'T> = MailboxProcessor<'T>

type CompositionRoot() =
    let maximumCapacity = 10
    let reservations =
        Collections.Concurrent.ConcurrentBag<Envelope<Reservation>>()

    let agent = new Agent<Envelope<MakeReservation>>(fun inbox ->
        let rec loop () = 
            async{
                let! command = inbox.Receive()
                let res = reservations |> ToReservations
                let handle = Handle maximumCapacity res
                let newReservation = handle command
                match newReservation with
                | Some(r) -> reservations.Add r
                | None -> ()
                return! loop()
            }
        loop())
    do agent.Start()

    interface IHttpControllerActivator with
        member this.Create(request, controllerDescriptor, controllerType) =
            if controllerType = typeof<HomeController> then
                new HomeController() :> IHttpController
            elif controllerType = typeof<ReservationController> then
                let c = new ReservationController()
                let subscription = c.Subscribe agent.Post
                request.RegisterForDispose subscription
                c :> IHttpController
            else
                raise
                <| ArgumentException(
                    sprintf "Unknown controller type requested: %O" controllerType,
                    "controllerType")

let ConfigureCompositionRoot (config : HttpConfiguration) =
    config.Services.Replace(
        typeof<IHttpControllerActivator>,
        CompositionRoot())

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
    ConfigureCompositionRoot c
    ConfigureFormatting c