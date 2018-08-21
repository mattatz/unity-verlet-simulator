unity-verlet-simulator
=====================

GPU-based simple verlet integration simulator for Unity.

![Tentacles](https://raw.githubusercontent.com/mattatz/unity-verlet-simulator/master/Captures/Tentacles.gif)

![GPUChainDemo](https://raw.githubusercontent.com/mattatz/unity-verlet-simulator/master/Captures/GPUChainDemo.gif)

![GPUClothDemo](https://raw.githubusercontent.com/mattatz/unity-verlet-simulator/master/Captures/GPUClothDemo.gif)

## Usage (chain structure example)

[SerializeField] ComputeShader compute; // GPUVerletSimulator.compute
GPUVerletSimulator simulator;

void Start() {
    const float edgeLength = 0.5f;

    // define nodes and edges.
    var nodes = new GPUNode[nodesCount];
    for(int i = 0; i < nodesCount; i++)
    {
        var n = nodes[i];
        var p = new Vector3(Random.value - 0.5f, i * edgeLength, Random.value - 0.5f);
        n.position = n.prev = p;
        n.decay = 1f;
        nodes[i] = n;
    }

    var edgesCount = nodesCount - 1;
    var edges = new GPUEdge[edgesCount];
    for(int i = 0; i < edgesCount; i++)
    {
        var e = edges[i];
        e.a = i;
        e.b = i + 1;
        e.length = edgeLength;
        edges[i] = e;
    }

    // create simulator instance.
    simulator = new GPUVerletSimulator(nodes, edges);
}

void Update() {
    const int iterations = 8;

    // simulate
    simulator.Step(compute);
    for(int i = 0; i < iterations; i++)
    {
        simulator.Solve(compute);
    }
}

## Sources

- Advanced Character Physics - http://web.archive.org/web/20080410171619/http://www.teknikus.dk/tj/gdc2001.htm

## Compatibility

tested on Unity 2018.2.4f, windows10 (GTX 1060).
