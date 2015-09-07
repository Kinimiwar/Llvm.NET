﻿struct foo
{
    int a;
    float b;
    int c[2];
};

struct foo bar = { 1, 2.0, { 3, 4 } };
struct foo baz;

inline static void copy( struct foo src     // function line here 
                       , struct foo* pDst
                       )
{ // function's ScopeLine here
    *pDst = src;
}

void DoCopy( )
{
    copy( bar, &baz );
}