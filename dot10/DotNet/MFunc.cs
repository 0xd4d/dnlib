namespace dot10.DotNet {
	delegate U MFunc<T, U>(T t);
	delegate V MFunc<T, U, V>(T t, U u);
}
