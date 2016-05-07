namespace BookingApi.WebHost

open System
open System.Collections.Concurrent
open System.Reactive
open System.Web.Http
open FSharp.Reactive
open BookingApi.Infrastructure
open BookingApi.Messages
open BookingApi.Domain
open BookingApi.Domain.Reservations
open BookingApi.Domain.Notifications

open Newtonsoft.Json
open System.IO

type ReservationsInFiles(directory : DirectoryInfo) =
    let toReservation (f: FileInfo) =
        let json = File.ReadAllText f.FullName
        JsonConvert.DeserializeObject<Envelope<Reservation>> json
    let toEnumerator (s:seq<'a>) = s.GetEnumerator()
    let getContainingDirectory (d : DateTime) =
        Path.Combine(
            directory.FullName,
            d.Year.ToString(),
            d.Month.ToString(),
            d.Day.ToString())
    let appendPath p2 p1 = Path.Combine(p1, p2)
    let getJsonFiles (dir : DirectoryInfo) =
        if Directory.Exists(dir.FullName) then
            dir.EnumerateFiles(
                "*.json", 
                SearchOption.AllDirectories)
        else
            Seq.empty
    member this.Write (reservation : Envelope<Reservation>) =
        let withExtension extension path = 
            Path.ChangeExtension(path, extension)
        let directoryName = 
            reservation.Item.Date 
            |> getContainingDirectory
        let fileName =
            directoryName
            |> appendPath (reservation.Id.ToString())
            |> withExtension "json"
        let json = JsonConvert.SerializeObject reservation

        Directory.CreateDirectory directoryName |> ignore
        File.WriteAllText(fileName, json)

    interface IReservations with
        member this.Between min max =
            Dates.InitInfinite min
            |> Seq.takeWhile (fun d -> d <= max)
            |> Seq.map getContainingDirectory
            |> Seq.collect 
                (fun dir -> 
                    DirectoryInfo(dir) 
                    |> getJsonFiles)
            |> Seq.map toReservation
        member this.GetEnumerator() =
            directory
            |> getJsonFiles
            |> Seq.map toReservation
            |> toEnumerator
        member this.GetEnumerator() =
            (this :> seq<Envelope<Reservation>>).GetEnumerator()
                :> System.Collections.IEnumerator

type NotificationsInFiles(directory : DirectoryInfo) =
    let toNotification(f : FileInfo) =
        let json = File.ReadAllText f.FullName
        JsonConvert.DeserializeObject<Envelope<Notification>>(json)
    let toEnumerator(s : seq<'a>) = s.GetEnumerator()
    let getContainingDirectory id =
        Path.Combine(directory.FullName, id.ToString())
    let appendPath p2 p1 = Path.Combine(p1, p2)
    let getJsonFiles (dir : DirectoryInfo) =
        if Directory.Exists(dir.FullName) then
            dir.EnumerateFiles("*.json", SearchOption.AllDirectories)
        else
            Seq.empty

    member this.Write (notification : Envelope<Notification>) =
        let withExtension extension path = 
            Path.ChangeExtension(path, extension) 
        let directoryName = notification.Item.About |> getContainingDirectory
        let fileName =
            directoryName
            |> appendPath (notification.Id.ToString())
            |> withExtension "json"
        let json = JsonConvert.SerializeObject notification

        Directory.CreateDirectory directoryName |> ignore
        File.WriteAllText(fileName, json)

    interface INotifications with
        member this.About id =
            id
            |> getContainingDirectory
            |> DirectoryInfo
            |> getJsonFiles
            |> Seq.map toNotification
        member this.GetEnumerator() =
            directory
            |> getJsonFiles
            |> Seq.map toNotification
            |> toEnumerator
        member this.GetEnumerator() =
            (this :> seq<Envelope<Notification>>).GetEnumerator()
                :> System.Collections.IEnumerator

type ErrorsInFiles(rootDirectory : string) =
    let getPath (d : DateTime) =
        String.Join(
            "/",
            [
                d.Year.ToString()
                d.Month.ToString()
                d.Day.ToString()
            ])

    let appendPath p2 p1 = Path.Combine(p1, p2)
    let withExtension extension path = 
        Path.ChangeExtension(path, extension) 

    member this.Write (ex : Exception) =
        let directoryName = 
            rootDirectory
            |> appendPath (getPath DateTimeOffset.Now.Date)
        let fileName =
            directoryName
            |> appendPath (Guid.NewGuid().ToString())
            |> withExtension "txt"

        Directory.CreateDirectory directoryName |> ignore
        File.WriteAllText(fileName, ex.ToString())
    
    interface System.Web.Http.Filters.IExceptionFilter with
        member this.AllowMultiple = true
        member this.ExecuteExceptionFilterAsync(actionExecutedContext, 
                                                cancellationToken) = 
            System.Threading.Tasks.Task.Factory.StartNew(
                fun () -> this.Write (actionExecutedContext.Exception))
            
type Agent<'a> = MailboxProcessor<'a>

type Global() =
    inherit System.Web.HttpApplication()
    member this.Application_Start (sender : obj) (e : EventArgs) =
        let seatingCapacity = 10
        
        let rootDir = 
            System.Web.HttpContext.Current.Server.MapPath(
                "~/Storage")

        let resetStorage() =
            if Directory.Exists(rootDir) then
                rootDir
                |> DirectoryInfo
                |> (fun dir -> dir.Delete(true))

            Directory.CreateDirectory(rootDir) |> ignore

        resetStorage()

        let errorHandler = ErrorsInFiles(Path.Combine(rootDir, "Errors"))
        GlobalConfiguration.Configuration.Filters.Add errorHandler

        let reservations = 
            ReservationsInFiles(
                Path.Combine(rootDir, "Reservations") 
                |> DirectoryInfo)
        let notifications = 
            NotificationsInFiles(
                Path.Combine(rootDir, "Notifications")
                |> DirectoryInfo)
        
        let reservationSubject = new Subjects.Subject<Envelope<Reservation>>()
        reservationSubject.Subscribe reservations.Write |> ignore

        let notificationsSubject = new Subjects.Subject<Notification>()
        notificationsSubject
        |> Observable.map EnvelopWithDefaults
        |> Observable.subscribe notifications.Write ignore ignore
        |> ignore

        let agent = new Agent<Envelope<MakeReservation>>(fun inbox ->
            let buildSuccessNotification (command : Envelope<MakeReservation>) =
                {
                    About = command.Id
                    Type = "Success"
                    Message = 
                        sprintf
                            "Your reservation for %s was completed. See you soon!"
                            (command.Item.Date.ToString "dd/MM/yyyy")
                }
            let buildFailedNotification (command : Envelope<MakeReservation>) =
                {
                    About = command.Id
                    Type = "Failure"
                    Message = 
                        sprintf
                            "We are sorry to inform you that your reservation for %s could not be completed."
                            (command.Item.Date.ToString "dd/MM/yyyy")
                }
            let rec loop () = 
                async{ 
                    try
                        let! command = inbox.Receive()
                        let handle = Handle seatingCapacity reservations
                        let newReservation = handle command
                        match newReservation with
                        | Some(r) -> 
                            reservationSubject.OnNext r
                            notificationsSubject.OnNext (buildSuccessNotification command)
                        | None -> 
                            notificationsSubject.OnNext (buildFailedNotification command)
                        return! loop()
                    with
                        e -> errorHandler.Write e
                }
            loop())
        do agent.Start()

        Configure 
            reservations
            notifications
            (Observer.Create agent.Post)
            seatingCapacity
            GlobalConfiguration.Configuration