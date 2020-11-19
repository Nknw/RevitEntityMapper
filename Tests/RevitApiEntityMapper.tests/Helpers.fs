module Helpers
open System.IO
open System.Reflection
open RTF.Applications

let location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

let prj = Path.Combine([location;"prj.rvt"]|>List.toArray)
let prjCopy = Path.Combine([location;"prjc.rvt"]|>List.toArray)

let setUp () =
    let app = RevitTestExecutive.CommandData.Application
    let doc = app.ActiveUIDocument
    let copyAndOpen ()= 
        File.Copy(prj,prjCopy,true)
        app.OpenAndActivateDocument(prjCopy) |> ignore
    match doc.Document with
        |doc when doc.PathName = prjCopy -> doc.Close () |> ignore
        |doc -> copyAndOpen ()