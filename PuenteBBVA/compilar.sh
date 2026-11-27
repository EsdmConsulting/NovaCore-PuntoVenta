#!/bin/sh
# Compila el puente para Java 8 o superior (no necesita el .jar de BBVA).
set -e
cd "$(dirname "$0")"
rm -rf build
mkdir -p build/classes dist
javac --release 8 -encoding UTF-8 -d build/classes $(find src -name '*.java')
jar --create --file dist/puente-bbva.jar --main-class mx.novacore.puente.Main -C build/classes .
cp puente.properties.ejemplo dist/
cp iniciar.bat dist/ 2>/dev/null || true
mkdir -p dist/lib
echo "Listo: dist/puente-bbva.jar"
