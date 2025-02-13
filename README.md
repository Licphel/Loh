# Loh
A very simple script language, needs only less than 2000 lines of code to build.
## Loh Grammar
- Variables & Control flows
```
-- This is comment.

-- Import a library.
local os = require("lang/os.loh")

-- Local variable.
local var1 = 3

-- Const variable is persistent in the vm. But it is still changable.
const var2 = "Hello world!"
var2 = "Hello world?"

-- If block. There are two styles.
if var1 == 2 do
  os.print("A")
else if(var1 == 3) do
  os.print("B")
else do
  os.print("C")
end -- Hey do not forget this end!

-- While loop.
local v = 0
while v < 3 do
  v = v + 1
  os.print(v)
end

-- While loop with brackets.
v = 0
while(v < 3) do
  v = v + 1
  os.print(v)
end

-- For loop.
for i = 0, i < var1, i = i + 1 do
  os.print(var2)
end

-- For loop with brackets.
for(i = 0, i < var1, i = i + 1) do
  os.print(var2)
end
```
- Arrays & Tables
```
local os = require("lang/os.loh")

-- Create an array & operations.
local array = [1, 2, 3]
-- To set with an out-of-bound index will fill null values before it.
array[4] = "a" -- Now array[3] is 'null'.
os.print(array[4])

-- Create a table & operations.
local table = { x = 1, y = 2, z = 3 }
table.x = 2
table["x"] = 2
os.print(table["x"])
os.print(table.x)

-- Run through the whole table
foreach k, v in table do
  if k == "z" do break end -- Break statement
  os.print(k)
  os.print(v)
end
```
- Functions & Closures.
```
local os = require("lang/os.loh")
local i = 1

-- We must declare a function's accessibility (local or const) or it will be seen as a anonymous function expression!
local function fib(x)
  if(x <= 2) do return i end -- Here a local variable 'i' is captured.
  return fib(x - 1) + fib(x - 2)
end

local fn_ref = fib -- Function referrence
os.print(fn_ref(10))
```
- Dataonly  
Returns a dataonly table or array using a much faster dataonly parser. No variables, no functions, just like json.
```
-- Keyword 'dataonly' must be at the begin of the code body (however, comments before it is ok)
dataonly 
{
  key1 = "A",
  key2 = [
    1, 2, 3, 4
  ]
}
```
## How to run a loh file
Simply call like this. It returns a dynamic value so make type checks!
```
FileHandle handle = FileSystem.GetAbsolute(absolutePath);
var returnedValue = LohEngine.Exec(handle)
```
where you can get the file handle through FileSystem or just instantiate one implementation.
