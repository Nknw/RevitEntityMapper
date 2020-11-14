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
        | attr when isnull attr -> attr|> ignore 
        | attr -> builder.SetDocumentation attr.Description|>ignore
    builder


let fieldinit entity= 
    let builder = SchemaBuilder entity.guid
    builder.SetSchemaName entity.name |> ignore
    let props = entity.entityType.GetProperties(BindingFlags.Instance ||| BindingFlags.Public)
                |> List.ofArray 
                |> List.filter (fun p-> not << isnull <|(p.GetCustomAttribute<NotMappedAttribute>()))
    (builder,props)



let fieldMapper visitor (builder:SchemaBuilder) (ctx:EntityType*PropertyInfo) = 
    let (eType,info) = ctx
    let writeMeta builderMethod t = builderMethod (info.Name,t) |> writeMeta info

    let handleEntity builderMethod def = match visitor def.entityType with 
                                             |Success(_) ->  (writeMeta builderMethod typeof<Entity>).SetSubSchemaGUID def.guid |> ignore
                                                             Success(builder)
                                             |Failure(s) -> Failure(s)

    let handleCollection builderMethod = function
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


let body = visitorBuilder fieldinit (fun info->info.PropertyType) fieldMapper

let hightlevel = higthLevelVisitorBuilder body (fun builder-> builder.Finish())






