// C++/CLI code that's compiled to a mixed mode assembly by the tests (see CppCliTests): native code and data
// in the same file as managed code.

#pragma unmanaged

static int nativeCounter = 40;

int NativeAdd(int a, int b) {
	return a + b;
}

int* GetNativeCounter() {
	return &nativeCounter;
}

#pragma managed

namespace MixedMode {
	public enum class Color { Red, Green, Blue };

	public value struct Point {
		int X;
		int Y;
	};

	public interface class IShape {
		int Area();
	};

	public ref class Rectangle : IShape {
		int width;
		int height;
	public:
		Rectangle(int width, int height) : width(width), height(height) { }
		virtual int Area() { return width * height; }
		property int Width { int get() { return width; } }
	};

	public ref class Calculator abstract sealed {
	public:
		// Managed code that calls native code
		static int Add(int a, int b) {
			return NativeAdd(a, b);
		}

		// Reads native data through a pointer
		static int Counter() {
			return *GetNativeCounter() + 2;
		}

		generic<typename T> static T Echo(T value) {
			return value;
		}

		static int AreaOf(IShape^ shape) {
			try {
				return shape->Area();
			}
			catch (System::NullReferenceException^) {
				return -1;
			}
		}

		static System::String^ Describe(Color color, Point point) {
			return System::String::Format("{0} {1},{2}", color, point.X, point.Y);
		}
	};
}
