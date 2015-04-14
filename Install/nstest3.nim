
import strutils

# this is a comment

proc FtestProcedure(str1: string, v1: int, v2: float): int =
    echo str1
    return 1

var v3: string = "hey there"
var v4: int = 2
var v5: float = 5.0
var v6: float32 = 0x4a

var v7 = FtestProcedure("a string", 111, 4.444)

var vseq: seq[int] = @[1,2,3]

var v8: float64 = 2.2233333

echo v3.toUpper()