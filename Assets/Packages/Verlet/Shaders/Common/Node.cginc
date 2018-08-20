#ifndef __VERLET_NODE_COMMON_INCLUDED__
#define __VERLET_NODE_COMMON_INCLUDED__

struct Node
{
  float3 position;
  float3 prev;
  float decay;
  bool stable;
};

#endif
