namespace Autodesk.Revit.Mapper
open System

type MapperException (str) = 
    inherit Exception(str)

