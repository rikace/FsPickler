﻿namespace Nessos.FsPickler

    open System
    open System.IO
    open System.Collections.Generic

    open Newtonsoft.Json

    open Nessos.FsPickler.Utils


    type JsonPickleWriter internal (textWriter : TextWriter, omitHeader, indented, leaveOpen) =
        
        let jsonWriter = new JsonTextWriter(textWriter) :> JsonWriter
        do 
            jsonWriter.Formatting <- if indented then Formatting.Indented else Formatting.None
            jsonWriter.CloseOutput <- not leaveOpen

        let mutable currentValueIsNull = false

        let mutable depth = 0
        let arrayStack = new Stack<int> ()
        do arrayStack.Push Int32.MinValue

        // do not write tag if omitting header or array element
        let omitTag () = (omitHeader && depth = 0) || arrayStack.Peek() = depth - 1

        interface IPickleFormatWriter with
            
            member __.BeginWriteRoot (tag : string) =
                if omitHeader then () else

                jsonWriter.WriteStartObject()
                writePrimitive jsonWriter false "FsPickler" AssemblyVersionInformation.Version
                writePrimitive jsonWriter false "type" tag

            member __.EndWriteRoot () = 
                if not omitHeader then jsonWriter.WriteEnd()

            member __.BeginWriteObject (tag : string) (flags : ObjectFlags) =

                if not <| omitTag () then
                    jsonWriter.WritePropertyName tag

                if flags.HasFlag ObjectFlags.IsNull then
                    currentValueIsNull <- true
                    jsonWriter.WriteNull()
                else
                    jsonWriter.WriteStartObject()
                    depth <- depth + 1

                    if flags = ObjectFlags.None then ()
                    else
                        let flagCsv = mkFlagCsv flags
                        writePrimitive jsonWriter false "pickle flags" flagCsv

            member __.EndWriteObject () = 
                if currentValueIsNull then 
                    currentValueIsNull <- false
                else
                    depth <- depth - 1
                    jsonWriter.WriteEndObject()

            member __.BeginWriteBoundedSequence (tag : string) (length : int) =
                arrayStack.Push depth
                depth <- depth + 1

                writePrimitive jsonWriter false "length" length
                jsonWriter.WritePropertyName tag
                
                jsonWriter.WriteStartArray()

            member __.EndWriteBoundedSequence () =
                depth <- depth - 1
                arrayStack.Pop () |> ignore
                jsonWriter.WriteEndArray ()

            member __.BeginWriteUnBoundedSequence (tag : string) =
                if not <| omitTag () then
                    jsonWriter.WritePropertyName tag

                arrayStack.Push depth
                depth <- depth + 1

                jsonWriter.WriteStartArray()

            member __.WriteHasNextElement hasNext =
                if not hasNext then 
                    arrayStack.Pop () |> ignore
                    depth <- depth - 1
                    jsonWriter.WriteEndArray ()

            member __.WriteBoolean (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag value
            member __.WriteByte (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag value
            member __.WriteSByte (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag value

            member __.WriteInt16 (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag value
            member __.WriteInt32 (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag value
            member __.WriteInt64 (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag value

            member __.WriteUInt16 (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag value
            member __.WriteUInt32 (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag value
            member __.WriteUInt64 (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag value

            member __.WriteSingle (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag value
            member __.WriteDouble (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag value
            member __.WriteDecimal (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag (string value)

            member __.WriteChar (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag value
            member __.WriteString (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag value
            member __.WriteBigInteger (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag (string value)

            member __.WriteGuid (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag value
            member __.WriteDate (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag value
            member __.WriteTimeSpan (tag : string) value = writePrimitive jsonWriter (omitTag ()) tag (string value)

            member __.WriteBytes (tag : string) (value : byte []) = writePrimitive jsonWriter (omitTag ()) tag value

            member __.IsPrimitiveArraySerializationSupported = false
            member __.WritePrimitiveArray _ _ = raise <| NotSupportedException()

            member __.Dispose () = 
                jsonWriter.Flush () ; textWriter.Flush () ; (jsonWriter :> IDisposable).Dispose()