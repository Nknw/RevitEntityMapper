namespace Autodesk.Revit.Mapper
open System
open Autodesk.Revit.DB
open Autodesk.Revit.DB.ExtensibleStorage

[<AttributeUsage(AttributeTargets.Class)>]
[<AllowNullLiteral>]
type SchemaAttribute(guid:Guid,name:string)=
    inherit Attribute()  
    member this.Guid = guid
    member this.Name = name

[<AttributeUsage(AttributeTargets.Class|||AttributeTargets.Property)>]
[<AllowNullLiteral>]
type DocumentationAttribute(doc:string) =
    inherit Attribute()
    member this.Description = doc

[<AttributeUsage(AttributeTargets.Class|||AttributeTargets.Property)>]
[<AllowNullLiteral>]
type UnitAttibute(unitType:UnitType,displayType:DisplayUnitType) =
    inherit Attribute()
    member this.UnitType = unitType
    member this.DisplayType = displayType

[<AttributeUsage(AttributeTargets.Class)>]
[<AllowNullLiteral>]
type PermissionsAttribute (read:AccessLevel,write:AccessLevel,vendor:string) =
    inherit Attribute()
    member this.Read = read
    member this.Write = write
    member this.Vendor = vendor

    new (read) = PermissionsAttribute(read,AccessLevel.Public,String.Empty)
    new (write, vendor) = PermissionsAttribute(AccessLevel.Public,write,vendor)