﻿module CallPolly.Events

open System

module Constants =
    let [<Literal>] EventPropertyName = "cp"

type Event =
    | Isolated of policy: string
    | Broken of action: string

module internal Log =
    open Serilog.Events

    /// Attach a property to the log context to hold the metrics
    // Sidestep Log.ForContext converting to a string; see https://github.com/serilog/serilog/issues/1124
    let forEvent (value : Event) (log : Serilog.ILogger) =
        let enrich (e : LogEvent) = e.AddPropertyIfAbsent(LogEventProperty(Constants.EventPropertyName, ScalarValue(value)))
        log.ForContext({ new Serilog.Core.ILogEventEnricher with member __.Enrich(evt,_) = enrich evt })

    let actionIsolated (log: Serilog.ILogger) policyName actionName =
        let lfe = log |> forEvent (Isolated policyName)
        lfe.Warning("Circuit Isolated for {actionName} based on {policy} policy", actionName, policyName)
    let actionBroken (log: Serilog.ILogger) policyName actionName limits =
        let lfe = log |> forEvent (Broken actionName)
        lfe.Warning("Circuit Broken for {actionName} based on limits in {policy}: {@limits}", actionName, policyName, limits)

    let breaking (exn: exn) (actionName: string) (timespan: TimeSpan) (log : Serilog.ILogger) =
        log.Warning(exn, "Circuit Breaking Activated for {actionName} for {duration}", actionName, timespan)
    let breakingDryRun (exn: exn) (actionName: string) (timespan: TimeSpan) (log : Serilog.ILogger) =
        log.Warning(exn, "Circuit Breaking DRYRUN for {actionName} for {duration}", actionName, timespan)
    let halfOpen (actionName: string) (log : Serilog.ILogger) =
        log.Information("Circuit Pending Reopen for {actionName}", actionName)
    let reset (actionName: string) (log : Serilog.ILogger) =
        log.Information("Circuit Reset for {actionName}", actionName)