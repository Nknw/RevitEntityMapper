module FieldMapper
open System.Reflection
open System
open System.Collections.Generic
open Autodesk.Revit.DB.ExtensibleStorage
open Autodesk.Revit.Mapper
open System.ComponentModel.DataAnnotations.Schema
open Abstractions


let writeMeta (info:PropertyInfo) (builder:FieldBuilder) = 
    match info.GetCustomAttribute<DocumentationAttribute>() with 
        | null -> builder
        | attr -> builder.SetDocumentation attr.Description


let creatorinit entity=
    match Schema.Lookup entity.guid with
    | null -> let builder = SchemaBuilder entity.guid
              builder.SetSchemaName entity.name |> ignore
              builder.SetWriteAccessLevel(AccessLevel.Public)|>ignore
              NeedsCreate(builder, getProps entity)
    |s -> s |> Success |> Complited



let creatorBody visitor (builder:SchemaBuilder) (ctx:EntityType*PropertyInfo) = 
    let (eType,info) = ctx
    let writeMeta builderMethod t = builderMethod (info.Name,t) |> writeMeta info

    let handleEntity builderMethod def = 
        visitor def |> continueSuccess (fun _ -> (writeMeta builderMethod typeof<Entity>).SetSubSchemaGUID def.guid |> ignore
                                                 builder|> Success)
    let handleCollection builderMethod = 
        function
            |ValueType(tp)->writeMeta builderMethod tp |> ignore
                            Success(builder)
            |EntityType(def)->handleEntity builderMethod def

    match eType with 
        |Simple(t) -> writeMeta builder.AddSimpleField t |> ignore
                      Success(builder)
        |Array(t) -> handleCollection builder.AddArrayField t
        |Entity(def) -> handleEntity builder.AddSimpleField def
        |Map(key,value) -> handleCollection (fun (s,t)->builder.AddMapField(s,key,t)) value
        |_ -> Failure("Unhandled")


let visitor = visitorBuilder creatorinit creatorBody (fun builder->builder.Finish()|> Success)

let creator : Type -> Schema Result =  visitor |> higthLevelVisitorBuilder
