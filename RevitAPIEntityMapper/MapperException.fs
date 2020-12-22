namespace Revit.EntityMapper
open System

type MapperException (str) = 
    inherit Exception(str)

    new (s, objs) = MapperException(String.Format(s, List.toArray objs))

