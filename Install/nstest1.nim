
import strutils, nstest2

proc ftst1(str1: string, v1: float, v2: int): int =
    echo "ftst1:str1: ",str1
    echo "ftst1:v1: ", v1
    echo "ftst1:v2: ", v2
    return 1

proc ftst1(str1: string, v1: string, v2: string): int =
    echo "ftst1:str1: ",str1
    echo "ftst1:v1: ", v1
    echo "ftst1:v2: ", v2
    return 1

proc ftst1(str1: string, v1: string, v2: int): int =
    ## help ftst1 line 1
    ## help ftst1 line 2
    echo "ftst1:str1: ",str1
    echo "ftst1:v1: ", v1
    echo "ftst1:v2: ", v2
    return 1

var retval = ftst1("hey1","hey2",111)
retval = ftst1("hey",1.1,222)
retval = ftst2("abc") # from nstest2

var str1 = "this word1 word2"
echo "str1: ",str1

var str2 = str1.replace("word1","replaced").toUpper()
echo "str2: ",str2

var i1=1
echo "i1: ", i1
i1+=1
echo "i1: ", i1
