CXX = g++
CXXFLAGS = -std=c++11 -Wall -Wextra -Isrc

fd: src/main.cpp $(wildcard src/*.h)
	$(CXX) $(CXXFLAGS) -o fd src/main.cpp

run: fd
	./fd

clean:
	rm -f fd
