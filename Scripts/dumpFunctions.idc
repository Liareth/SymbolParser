#include <idc.idc>

static FuncDump(start)
{
    auto ea, str;
    ea = start;

    while(ea != BADADDR)
    {
        str = GetFunctionName(ea);
        Message("%s 0x%X\n", Demangle(str, GetLongPrm(INF_LONG_DN)), ea);
        ea = NextFunction(ea);
    }
}

static main()
{
    FuncDump(0x00401000);
}