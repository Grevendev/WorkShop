# CAIN Skills File — Complaint Processing Logic

## Parsing a log entry

Each complaint line has six pipe-separated fields:

`#NNN | YYYY-MM-DD HH:MM:SS | SEVERITY | CLIENT | CITY | DESCRIPTION`

Before applying any rule, always extract:

- **SEVERITY**: read directly from the entry; no tool needed.  
- **DATE**: the `YYYY-MM-DD` part only (pass this as the date tool argument).  
- **CITY**: the city name (pass this as the weather tool location argument).  

These values are reused across all rules.

---

## Rule 1 — Skip PANIC severity

1. Read the **SEVERITY** field.  
2. If the SEVERITY field is exactly `PANIC`, **SKIP** this entry.  
3. Do not call any tools for this rule.  
4. Move to the next log entry.

---

## Rule 2 — Skip entries on Mondays

1. Call the **weekday-date** tool with:
   - `date = DATE`
2. The date tool returns text like:

   `Saturday, 1st`

3. Read the **first word** of the response to get the weekday name.  
4. If the first word is `"Monday"`, **SKIP** this entry.  
5. Otherwise, continue to the next rule.

---

## Rule 3 — Skip rainy weekends

1. Call the **weekday-date** tool with:
   - `date = DATE`
2. Read the first word of the response:
   - If it is `"Saturday"` or `"Sunday"`, continue this rule.
   - Otherwise, stop this rule and continue to the next rule.
3. Call the **weather-forecast** tool with:
   - `city = CITY`
   - `date = DATE`
4. The weather tool returns text like:

   `16 degrees C, overcast with heavy rain, 8.7mm precipitation`

5. In the weather response:
   - Find the number immediately before the text `"mm precipitation"`.
   - Interpret this number as the precipitation amount in millimetres.
6. If the precipitation amount is **greater than 1**, **SKIP** this entry.  
7. Otherwise, continue to the next rule.

---

## Rule 4 — Skip extreme heat

1. Call the **weather-forecast** tool with:
   - `city = CITY`
   - `date = DATE`
2. The weather tool returns text like:

   `Weather in Rome on 2025-07-22: Sunny, max 41°C`

3. In the weather response:
   - Find the number immediately before the text `"°C"`.
   - Interpret this number as the maximum temperature in degrees Celsius.
4. If the maximum temperature is **greater than or equal to 40**, **SKIP** this entry.  
5. Otherwise, continue to the next rule.

---

## Rule 5 — Saturn bonus rule (date range skip)

1. Use the extracted **DATE** field in `YYYY-MM-DD` format.  
2. If DATE is between **2025-08-22** and **2025-09-20** (inclusive), **SKIP** this entry.  
3. Otherwise, continue to the next rule.

---

## Rule 6 — Default handling

If none of the above rules caused the entry to be skipped:

- Mark the entry as **HANDLE**.  
- Proceed with normal complaint processing.

---

# Analytics MCP — Usage Notes

The analytics MCP server is available at:

`https://tribetrot.ngrok.app/api/analytics-mcp`

Register this as an HTTP MCP server in your AI assistant. After restart, the following tools are available:

- `list_severities()`  
  - Lists all distinct severity levels present in the log.

- `count_complaints_by(field)`  
  - Counts entries grouped by a given field: `city`, `severity`, `weekday`, or `month`.

- `search_complaints(city?, severity?, limit?)`  
  - Filters entries by `city`, `severity`, or both, with an optional `limit`.

## CEO questions to answer using the analytics MCP

1. **Which city in our client base generates the most complaints?**  
   - Use `count_complaints_by("city")` and pick the city with the highest count.

2. **How many PANIC-severity entries are in the log?**  
   - Use `search_complaints(severity="PANIC")` and count the returned entries,  
     or use `count_complaints_by("severity")` and read the count for `PANIC`.

3. **Which severity level is most common overall?**  
   - Use `count_complaints_by("severity")` and select the severity with the highest count.

Write the three answers as one sentence each and submit them as required.
