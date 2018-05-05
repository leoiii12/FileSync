OUT = $(shell pwd)/publish
SRC = $(wildcard File/Sync/*.cs)

all: build

build: $(SRC)
	dotnet publish FileSync -c Release -o $(OUT)/osx-x64 -r osx-x64
	dotnet publish FileSync -c Release -o $(OUT)/win-x86 -r win-x86

clean:
	rm -rf $(OUT)
	rm -rf FileSync/logs/
	rm -rf FileSync/bin/