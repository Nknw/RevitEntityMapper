module FieldMapper
open System.Reflection
open System
open Autodesk.Revit.DB
open Autodesk.Revit.DB.ExtensibleStorage
open Autodesk.Revit.Mapper
open Abstractions

type CreatorContext = {
    defaultUT : Option<UnitType>
    builder : SchemaBuilder
    }

let writeMeta defaultUT (info:PropertyInfo) (builder:FieldBuilder) = 
    info 
     |> fetchAttribute<DocumentationAttribute> 
     (fun attr -> attr.Description |> builder.SetDocumentation |> ignore)
    match builder.NeedsUnits() with
    |false -> builder
    |true -> match info.GetCustomAttribute<UnitAttribute>() with
             |null -> match defaultUT with
                      |Some(dut) -> builder.SetUnitType dut
                      |Option.None -> (log "prop {0} must have attribute unit" [info.Name]) |> failwith
             |attr -> builder.SetUnitType attr.UnitType

let getCreatorContext (builder:SchemaBuilder) (t:Type) = 
    match t.GetCustomAttribute<UnitAttribute> () with
    |null -> { builder=builder ; defaultUT = Option.None }
    |attr -> { builder=builder ; defaultUT = Some(attr.UnitType) }

let writeSchemaBuilderMeta (builder:SchemaBuilder) (t:Type) =
    t |> fetchAttribute<DocumentationAttribute> 
         (fun attr -> builder.SetDocumentation(attr.Description) |> ignore)
    t |> fetchAttribute<PermissionsAttribute> 
         (fun attr -> match attr.Write with
                      |AccessLevel.Public -> builder.SetReadAccessLevel(attr.Read) |>ignore
                      |lvl -> builder.SetReadAccessLevel(attr.Read)
                               .SetVendorId(attr.Vendor)
                               .SetWriteAccessLevel(lvl)
                               |> ignore)

let creatorinit entity=
    match Schema.Lookup entity.guid with
    | null -> let builder = SchemaBuilder entity.guid
              let t = entity.entityType
              builder.SetSchemaName entity.name |> ignore
              builder.SetWriteAccessLevel(AccessLevel.Public)|>ignore
              writeSchemaBuilderMeta builder t
              NeedsCreate(getCreatorContext builder t, getProps entity)
    |s -> s |> Success |> Complited

let creatorBody visitor ctx ((eType,info):EntityType*PropertyInfo) = 
    let writeMeta builderMethod t = builderMethod (info.Name,t) |> writeMeta ctx.defaultUT info 

    let handleEntity builderMethod def = 
        visitor def 
        |> continueSuccess (fun _ -> (writeMeta builderMethod typeof<Entity>).SetSubSchemaGUID def.guid |> ignore
                                     ctx|> Success)
    let handleCollection builderMethod = 
        function
        |ValueType(tp)->writeMeta builderMethod tp |> ignore
                        Success(ctx)
        |EntityType(def)->handleEntity builderMethod def

    match eType with 
    |Simple(t) -> writeMeta ctx.builder.AddSimpleField t |> ignore
                  Success(ctx)
    |Array(t) -> handleCollection ctx.builder.AddArrayField t
    |Entity(def) -> handleEntity ctx.builder.AddSimpleField def
    |Map(key,value) -> handleCollection (fun (s,t)-> ctx.builder.AddMapField(s,key,t)) value
    |_ -> Failure("Unhandled")

let visitor = visitorBuilder creatorinit creatorBody (fun ctx -> ctx.builder.Finish() |> Success)

let creator : Type -> Schema Result =  visitor |> higthLevelVisitorBuilder
