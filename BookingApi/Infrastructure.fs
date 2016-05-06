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
open BookingApi.Domain.Notifications

type CompositionRoot(reservations: IReservations, 
                     notifications: INotifications,
                     reservationRequestObserver,
                     seatingCapacity) =
    interface IHttpControllerActivator with
        member this.Create(request, controllerDescriptor, controllerType) =
            if controllerType = typeof<HomeController> then
                new HomeController() :> IHttpController
            elif controllerType = typeof<ReservationsController> then
                let c = new ReservationsController()
                c
                |> Observable.subscribeObserver reservationRequestObserver
                |> request.RegisterForDispose
                c :> IHttpController
            elif controllerType = typeof<NotificationsController> then
                new NotificationsController(notifications) :> IHttpController
            elif controllerType = typeof<AvailabilityController> then
                new AvailabilityController(reservations, seatingCapacity) :> IHttpController
            else
                raise
                <| ArgumentException(
                    sprintf "Unknown controller type requested: %O" controllerType,
                    "controllerType")

let ConfigureCompositionRoot 
    reservations 
    notifications 
    reservationRequestObserver 
    seatingCapacity
    (config : HttpConfiguration) =
        config.Services.Replace(
            typeof<IHttpControllerActivator>,
            CompositionRoot(
                reservations, 
                notifications, 
                reservationRequestObserver,
                seatingCapacity))

type HttpRouteDefaults = { Controller : string; Id : obj }

let ConfigureRoutes (configuration : HttpConfiguration) =
    configuration.Routes.MapHttpRoute(
        "AvailabilitiesYear",
        "Availability/{year}",
        { Controller = "Availability"; Id = RouteParameter.Optional })
    |> ignore
    configuration.Routes.MapHttpRoute(
        "AvailabilitiesMonth",
        "Availability/{year}/{month}",
        { Controller = "Availability"; Id = RouteParameter.Optional })
    |> ignore
    configuration.Routes.MapHttpRoute(
        "AvailabilitiesDay",
        "Availability/{year}/{month}/{day}",
        { Controller = "Availability"; Id = RouteParameter.Optional })
    |> ignore
    configuration.Routes.MapHttpRoute(
            "DefaultAPI",
            "{controller}/{id}",
            { Controller = "Home"; Id = RouteParameter.Optional })
    |> ignore

let ConfigureFormatting (configuration : HttpConfiguration) =
    configuration.Formatters.JsonFormatter.SerializerSettings.ContractResolver
        <- Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()

let Configure 
        reservations 
        notifications 
        reservationRequestObserver 
        seatingCapacity
        c = 
    ConfigureRoutes c
    ConfigureCompositionRoot 
        reservations 
        notifications 
        reservationRequestObserver 
        seatingCapacity
        c
    ConfigureFormatting c