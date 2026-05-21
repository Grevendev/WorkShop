# C# MCP Server: **date-info** Tool

## Overview
This project implements a fully compliant Model Context Protocol (MCP)
server in C#
The server exposes a single tool, 'date-info', which accepts an ISO-formatted date (YYYY-MM-DD) and returns:
- The wwekday name (e.g., Monday, Saturday)
- The ordinal occurrence of that weekday within the month (e.g., 1st Monday, 3rd Saturday)
This tool is designed to pass the official MCP Challange #1 verification.

---

## Features
- Full MCP protocol support:
- - initialize
- - tool/list
- - tools/Call

- Implements the required date arithmetic without external dependencies
- Clean JSON-RPC-style request/response handling
- Fully compatible with:
- - Claude Desktop
- - Cursor
- Copilor
- - Any MCP-enabled client

--- 

## Tool: **'date-info'**

## Description
Returns the weekday and ordinal occurrence for a given ISO date.
