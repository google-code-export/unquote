﻿namespace Swensen.Unquote

open Swensen.Utils
open Swensen.Unquote.PrecompiledRegexes

module RegexPatterns =
    ///Match the numeric literal module name pattern and extract the suffix
    let (|NumericLiteral|_|) =
        let regex = Swensen.Unquote.PrecompiledRegexes.NumericLiteralRegex()
        fun input ->
            match input with 
            | Regex.RegexMatch regex {GroupValues=[suffix]} -> Some(suffix)
            | _ -> None

    ///Match and extract the full type name short name
    let (|ShortName|_|) =
        let regex = ShortNameRegex()
        fun input ->
            match input with 
            | Regex.RegexMatch regex {GroupValues=[shortName]} -> Some(shortName)
            | _ -> None
