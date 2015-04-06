
import strutils

proc ftst2*(str1: string): int =
    echo "ftst2:str1: ",str1
    return 1

proc ftst2*(str1: string, v1: float, v2: int): int =
    echo "ftst2:str1: ",str1
    echo "ftst2:v1: ", v1
    echo "ftst2:v2: ", v2
    return 1

proc ftst2*(str1: string, v1: string, v2: string): int =
    echo "ftst2:str1: ",str1
    echo "ftst2:v1: ", v1
    echo "ftst2:v2: ", v2
    return 1

proc ftst2*(str1: string, v1: string, v2: int): int =
    ## ftst2 func help
    echo "ftst2:str1: ",str1
    echo "ftst2:v1: ", v1
    echo "ftst2:v2: ", v2
    return 1

