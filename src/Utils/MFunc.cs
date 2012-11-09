namespace dot10.Utils {
	delegate U MFunc<T, U>(T t);
	delegate V MFunc<T, U, V>(T t, U u);
}
