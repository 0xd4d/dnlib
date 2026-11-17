// dnlib: See LICENSE.txt for more info

#nullable enable

#if THREAD_SAFE
// .NET 9+ has System.Threading.Lock which conflicts with our Lock class
global using Lock = dnlib.Threading.Lock;
#endif
