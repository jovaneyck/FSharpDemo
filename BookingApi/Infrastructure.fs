module BookingApi.Infrastructure

open System
open System.Net.Http
open System.Reactive
open FSharp.Reactive
open FSharp.Control.Reactive
open System.Web.Http
open System.Web.Http.Dispatcher
open System.Web.Http.Controllers
open BookingApi.HttpApi
open BookingApi.Messages
open BookingApi.Domain.Reservations

type CompositionRoot(reservations: IReservations, reservationRequestObserver) =
    interface IHttpControllerActivator with
        member this.Create(request, controllerDescriptor, controllerType) =
            if controllerType = typeof<HomeController> then
                new HomeController() :> IHttpController
            elif controllerType = typeof<ReservationController> then
                let c = new ReservationController()
                c
                |> Observable.subscribeObserver reservationRequestObserver
                |> request.RegisterForDispose
                c :> IHttpController
            else
                raise
                <| ArgumentException(
                    sprintf "Unknown controller type requested: %O" controllerType,
                    "controllerType")

let ConfigureCompositionRoot reservations reservationRequestObserver (config : HttpConfiguration) =
    config.Services.Replace(
        typeof<IHttpControllerActivator>,
        CompositionRoot(reservations, reservationRequestObserver))

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

let Configure reservations reservationRequestObserver c = 
    ConfigureRoutes c
    ConfigureCompositionRoot reservations reservationRequestObserver c
    ConfigureFormatting c