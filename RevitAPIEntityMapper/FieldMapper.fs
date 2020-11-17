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


let fieldinit entity=
    match Schema.Lookup entity.guid with
    | null -> let builder = SchemaBuilder entity.guid
              builder.SetSchemaName entity.name |> ignore
              NeedsCreate(builder, getProps entity)
    |s -> s |> Success |> Complited



let fieldMapperBody visitor (builder:SchemaBuilder) (ctx:EntityType*PropertyInfo) = 
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


let visitor = visitorBuilder fieldinit fieldMapperBody (fun builder->builder.Finish()|> Success)

let fieldMapper =  visitor |> higthLevelVisitorBuilder

let t = fieldMapper typeof<unit>






