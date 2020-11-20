module Helpers
open System.IO
open System.Reflection
open RTF.Applications
open Autodesk.Revit.DB.ExtensibleStorage
open Autodesk.Revit.DB
open System
open NUnit.Framework
open FieldMapper
open Abstractions
open System.Linq

let location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

let execInTransaction doc change =
    use tr = new Transaction(doc,"test")
    (tr.Start()) |> ignore
    change ()
    tr.Commit() |> ignore

let prj = Path.Combine([location;"prj.rvt"]|>List.toArray)

let assertThat (is:'a) (some:'a) =  Assert.That(some ,Is.EqualTo(is))

let fstGuid = "e7fd718b-d7f5-4dce-ac33-06e138749344" |> Guid
let sndGuid = "c7355511-675d-4079-903d-a0684d8d05d1" |> Guid

let assertFstGuid = assertThat fstGuid
let assertSndGuid = assertThat sndGuid


let hasTypes (s:Schema) (key,value) = 
    let field = s.ListFields().First()
    let checkValue () = field.ValueType |> assertThat value
    match field.ContainerType with
     |ContainerType.Map -> checkValue ()
                           field.KeyType |> assertThat key
     |ContainerType.Array -> checkValue ()
     |ContainerType.Simple -> checkValue()

let hasType  s t = 
    hasTypes s (typeof<unit>,t)

let testCreatorWith should t = 
    match creator t with
    |Success(s) -> should(s)
    |Failure(s) -> failwith(s)

let setUp () = 
    let app = RevitTestExecutive.CommandData.Application
    let doc = app.ActiveUIDocument
    let checkSchema guid = 
        match Schema.Lookup(guid) with
        |null-> null |> ignore
        |sc -> failwith (sc.GUID.ToString()+"exists")
    let checkAll () = fstGuid |> checkSchema
                      sndGuid |> checkSchema  
    match doc.Document with
    |doc when doc.PathName = prj -> checkAll ()
    |doc -> app.OpenAndActivateDocument(prj) |> ignore
            checkAll ()

let boolT = typeof<bool>
let strT = typeof<string>