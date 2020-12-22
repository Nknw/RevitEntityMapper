module Visitor
open TypeResolver
open System.Reflection
open Revit.EntityMapper

type InitResult<'result,'state> = 
    |Complited of 'result
    |NeedsCreate of 'state 

let getPropType (prop:PropertyInfo) = prop.PropertyType

type StepContext<'result,'state> = {
    info : PropertyInfo
    visitor : EntityDef -> 'result 
    eType : EntityType
    stepState : 'state
    }

let visitorBuilder init step finallize =
    let rec visitor entity =
        let foldProps state propInfo  =  
            match propInfo |> getPropType |> Init |> getType with
            | Init(_) -> raise (new MapperException("no one handle input"))
            | Entity(_) | Map(_) | Array(_) | Simple(_) as t -> { info = propInfo; visitor = visitor;
                                                                  eType = t; stepState = state; 
                                                                  } |> step
          
        match entity |> init with 
        | Complited(result) -> result
        | NeedsCreate(state) -> (state,getProps entity) ||> List.fold foldProps |> finallize
    visitor

let getEntityDefenition t =
    match t |> Init |> isEntity  with
    | Entity(e) -> e  
    | _ ->  raise (new MapperException("{0} has no SchemaAttribute", [t]))
