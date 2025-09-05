#!/usr/bin/env python3
"""
Convert PLY mesh files to OBJ format with proper material assignments.
"""

import argparse
import numpy as np
import open3d as o3d
import os
import sys

def convert_ply_to_obj(ply_file, obj_file, mtl_file=None):
    """
    Convert PLY mesh to OBJ format.
    
    Args:
        ply_file (str): Path to input PLY file
        obj_file (str): Path to output OBJ file
        mtl_file (str): Path to output MTL file (optional)
    """
    
    # Load PLY file
    print(f"Loading PLY file: {ply_file}")
    mesh = o3d.io.read_triangle_mesh(ply_file)
    
    if len(mesh.vertices) == 0:
        print("Error: No vertices found in PLY file")
        return False
    
    print(f"Loaded mesh with {len(mesh.vertices)} vertices and {len(mesh.triangles)} triangles")
    
    # Create output directory if it doesn't exist
    os.makedirs(os.path.dirname(obj_file), exist_ok=True)
    
    # Generate MTL file if not provided
    if mtl_file is None:
        mtl_file = obj_file.replace('.obj', '.mtl')
    
    # Write OBJ file
    print(f"Writing OBJ file: {obj_file}")
    with open(obj_file, 'w') as f:
        f.write(f"# Converted from {ply_file}\n")
        f.write(f"mtllib {os.path.basename(mtl_file)}\n")
        f.write(f"o {os.path.splitext(os.path.basename(obj_file))[0]}\n")
        
        # Write vertices
        for vertex in mesh.vertices:
            f.write(f"v {vertex[0]:.6f} {vertex[1]:.6f} {vertex[2]:.6f}\n")
        
        # Write vertex normals if available
        if mesh.has_vertex_normals():
            for normal in mesh.vertex_normals:
                f.write(f"vn {normal[0]:.6f} {normal[1]:.6f} {normal[2]:.6f}\n")
        
        # Write texture coordinates if available
        if mesh.has_vertex_colors():
            # Convert colors to texture coordinates (simplified)
            for i, color in enumerate(mesh.vertex_colors):
                # Use color as simple texture coordinates
                u = color[0]  # Red as U
                v = color[1]  # Green as V
                f.write(f"vt {u:.6f} {v:.6f}\n")
        
        # Write faces
        f.write(f"usemtl material_0\n")
        for triangle in mesh.triangles:
            # OBJ uses 1-based indexing
            v1 = triangle[0] + 1
            v2 = triangle[1] + 1
            v3 = triangle[2] + 1
            
            if mesh.has_vertex_colors() and mesh.has_vertex_normals():
                f.write(f"f {v1}/{v1}/{v1} {v2}/{v2}/{v2} {v3}/{v3}/{v3}\n")
            elif mesh.has_vertex_normals():
                f.write(f"f {v1}//{v1} {v2}//{v2} {v3}//{v3}\n")
            else:
                f.write(f"f {v1} {v2} {v3}\n")
    
    # Write MTL file
    print(f"Writing MTL file: {mtl_file}")
    with open(mtl_file, 'w') as f:
        f.write("# Material file for converted mesh\n")
        f.write("newmtl material_0\n")
        f.write("Ka 0.2 0.2 0.2\n")  # Ambient color
        f.write("Kd 0.8 0.8 0.8\n")  # Diffuse color
        f.write("Ks 0.0 0.0 0.0\n")  # Specular color
        f.write("Ns 0.0\n")          # Specular exponent
        f.write("d 1.0\n")           # Dissolve (transparency)
        f.write("illum 2\n")         # Illumination model
    
    print("Conversion completed successfully!")
    return True

def main():
    parser = argparse.ArgumentParser(description='Convert PLY mesh to OBJ format')
    parser.add_argument('input', help='Input PLY file')
    parser.add_argument('output', help='Output OBJ file')
    parser.add_argument('--mtl', help='Output MTL file (optional)')
    
    args = parser.parse_args()
    
    if not os.path.exists(args.input):
        print(f"Error: Input file {args.input} does not exist")
        sys.exit(1)
    
    success = convert_ply_to_obj(args.input, args.output, args.mtl)
    
    if not success:
        sys.exit(1)

if __name__ == "__main__":
    main()
