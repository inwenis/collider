# Particle Collider

![Sample screenshot](https://github.com/inwenis/collider/blob/master/screenshot.gif)

This is a particle collider I'm coding for fun and as an exercise.
- deliberately not using TDD
- deliberately using shorter variable names
- goal: play with profiling and optimizations
- goal: come back and assess this code in 1/2 year
- goal: create a heat distribution simulation of decent size
- collisions formulas are from: https://introcs.cs.princeton.edu/java/assignments/collisions.html

## How to run?
Just build it and run Collider.exe
```
> Collider.exe
Particles saved to 2020-07-22--06-58-56.csv. To rerun use: --file=2020-07-22--06-58-56.csv
  1% passed=00:00:00.0260049 total estimated=00:00:02.6000000 remaining=00:00:02.5739951
  2% passed=00:00:00.0276062 total estimated=00:00:01.3800000 remaining=00:00:01.3523938
  3% passed=00:00:00.0280174 total estimated=00:00:00.9340000 remaining=00:00:00.9059826
...
 98% passed=00:00:00.0881926 total estimated=00:00:00.0900000 remaining=00:00:00.0018074
 99% passed=00:00:00.0887246 total estimated=00:00:00.0900000 remaining=00:00:00.0012754
100% passed=00:00:00.0894271 total estimated=00:00:00.0890000 remaining=-00:00:00.0004271
Printing frames
  1% passed=00:00:00.0553889 total estimated=00:00:05.5390000 remaining=00:00:05.4836111
  2% passed=00:00:00.0681729 total estimated=00:00:03.4090000 remaining=00:00:03.3408271
  3% passed=00:00:00.0837812 total estimated=00:00:02.7930000 remaining=00:00:02.7092188
...
 98% passed=00:00:01.5977113 total estimated=00:00:01.6300000 remaining=00:00:00.0322887
 99% passed=00:00:01.6180641 total estimated=00:00:01.6340000 remaining=00:00:00.0159359
100% passed=00:00:01.6489969 total estimated=00:00:01.6490000 remaining=00:00:00.0000031
(here you'll get a ugly WinForms app with the simulation)
```
## What is the Tests project for?
To compare, measure and profile different optimizations.

## CLI - Command Line Interface
CLI for Collider.exe
```
-f, --frames       (Default: 1000) Number of frames the simulation should contain.
-n, --particles    (Default: 100) Number of particles in the simulation
-r, --radius       (Default: 5) Radius of particles in pixels
-i, --file         Input file with inputs, see below for format. Running a simulation
                   without `-i` outputs a CSV file so you can rerun simulations
-s, --size         (Default: 400 400) Size of the surface in pixels
--help             Display this help screen.
--version          Display version information.
```

## Input file format
Sample input file:
```
NumberOfFrames=1000
size=400,400
             positionX|            positionY|                   velocityX|                   velocityY|  mass|radius
 200.0                | 200.0               |  0.0                       |  0.0                       | 20.0 |   20
 328.0845947265625    |  64.564788818359375 | -0.13202393054962158203125 | -1.94569146633148193359375 |  1.0 |    5
 247.4674835205078125 | 226.641265869140625 |  0.65267336368560791015625 | -1.17782962322235107421875 |  1.0 |    5
```

## Examples
```
> Collider.exe --frames 5000 --size 800 800 -n 200

> Collider.exe --frames 5000 --size 800 800 -n 200 -r 6

> Collider.exe -i input.csv
```
