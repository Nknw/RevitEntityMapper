module Creator
open TypeResolver
open Visitor
open System.Reflection
open System
open Autodesk.Revit.DB
open Autodesk.Revit.DB.ExtensibleStorage
open Autodesk.Revit.Mapper

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
                      |Option.None -> raise(new MapperException(log "prop {0} or class must have UnitAttribute" [info.Name]))
             |attr -> builder.SetUnitType attr.UnitType

let getCreatorContext (builder:SchemaBuilder) (t:Type) = 
    match t.GetCustomAttribute<UnitAttribute> () with
    |null -> { builder=builder ; defaultUT = None }
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
              writeSchemaBuilderMeta builder t
              NeedsCreate(getCreatorContext builder t)
    |s -> s |> Complited

let creatorBody ctx = 
    let writeMeta builderMethod t = builderMethod (ctx.info.Name,t) |> writeMeta ctx.stepState.defaultUT ctx.info 

    let handleEntity builderMethod def = 
        ctx.visitor def |> ignore
        (writeMeta builderMethod typeof<Entity>).SetSubSchemaGUID def.guid |> ignore
        ctx.stepState
    let handleCollection builderMethod = 
        function
        |ValueType(tp)->writeMeta builderMethod tp |> ignore
                        ctx.stepState
        |EntityType(def)->handleEntity builderMethod def

    match ctx.eType with 
    |Simple(t) -> writeMeta ctx.stepState.builder.AddSimpleField t |> ignore
                  ctx.stepState
    |Array(t) -> handleCollection ctx.stepState.builder.AddArrayField t
    |Entity(def) -> handleEntity ctx.stepState.builder.AddSimpleField def
    |Map(key,value) -> handleCollection (fun (s,t)-> ctx.stepState.builder.AddMapField(s,key,t)) value
    |_ ->  raise (new MapperException("no one handle input"))

let creator = visitorBuilder creatorinit creatorBody (fun ctx -> ctx.builder.Finish())

