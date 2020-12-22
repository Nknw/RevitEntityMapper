module Helpers
open System.IO
open System.Reflection
open RTF.Applications
open Autodesk.Revit.DB.ExtensibleStorage
open Autodesk.Revit.DB
open System.Collections.Generic
open NUnit.Framework
open Creator
open Visitor
open GetterBuilder
open Revit.EntityMapper
open System.Linq
open SetterBuilder

let location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
let app = RevitTestExecutive.CommandData.Application

let execInTransaction change =
    use tr = new Transaction(app.ActiveUIDocument.Document,"test")
    (tr.Start()) |> ignore
    change ()
    tr.Commit() |> ignore

let getWall () = let collector =  FilteredElementCollector(app.ActiveUIDocument.Document)
                 collector.OfClass(typeof<Wall>).FirstElement()

let prj = Path.Combine([location;"prj.rvt"]|>List.toArray)

let assertThat (is:'a) (some:'a) =  Assert.That(some ,Is.EqualTo(is))
let assertThasEquivalent (is:'a) (some:'a) = Assert.That(some, Is.EqualTo(is))

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
    t |> getEntityDefenition |> creator |> should

let testGetterWith<'t> should =
    let t = typeof<'t>
    let def = getEntityDefenition t
    let schema = creator def
    getterBuilder (Dictionary()) def :?> Entity->'t |> should schema
    ()

let testSetterWith<'t> should =
    let t = typeof<'t>
    let def = getEntityDefenition t 
    let schema = creator def 
    setterBuilder (Dictionary()) def :?>'t -> Entity |> should schema
    ()

let setUp () = 
    let doc = app.ActiveUIDocument.Document
    let checkSchema guid = 
        match Schema.Lookup(guid) with
        |null-> null |> ignore
        |sc -> failwith (sc.GUID.ToString()+"exists")
    let checkAll () = 
        typeof<ReflectedClasses.Bool>.Assembly.GetTypes() 
        |> Seq.map (fun t-> t.GetCustomAttribute<SchemaAttribute>())
        |> Seq.filter (fun attr -> not << isNull <| attr)
        |> Seq.map (fun attr -> checkSchema attr.Guid)
    match doc with
    |doc when doc.PathName = prj -> ignore ()
    |doc -> app.OpenAndActivateDocument(prj) |> ignore
            checkAll () |> ignore

let boolT = typeof<bool>
let strT = typeof<string>
