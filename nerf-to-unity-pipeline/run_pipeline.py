#!/usr/bin/env python3
"""
Complete pipeline to convert NeRF mesh to Unity-ready assets.
"""

import argparse
import os
import sys
import subprocess
import json
from pathlib import Path

def run_command(command, cwd=None):
    """Run a shell command and return the result."""
    try:
        result = subprocess.run(command, shell=True, cwd=cwd, capture_output=True, text=True)
        if result.returncode != 0:
            print(f"Error running command: {command}")
            print(f"Error output: {result.stderr}")
            return False
        return True
    except Exception as e:
        print(f"Exception running command: {command}")
        print(f"Exception: {e}")
        return False

def convert_ply_to_obj(ply_file, obj_file):
    """Convert PLY to OBJ format."""
    print(f"Converting {ply_file} to {obj_file}")
    return run_command(f"python convert_ply_to_obj.py {ply_file} {obj_file}")

def generate_collision_mesh(obj_file, collision_file):
    """Generate simplified collision mesh."""
    print(f"Generating collision mesh: {collision_file}")
    return run_command(f"python generate_collision_mesh.py {obj_file} {collision_file}")

def optimize_mesh(obj_file, optimized_file):
    """Optimize mesh for Unity."""
    print(f"Optimizing mesh: {optimized_file}")
    return run_command(f"python optimize_mesh.py {obj_file} {optimized_file}")

def copy_to_unity(input_dir, unity_dir):
    """Copy processed files to Unity project."""
    print(f"Copying files to Unity project: {unity_dir}")
    
    # Create Unity directories
    models_dir = Path(unity_dir) / "Assets" / "Models"
    textures_dir = Path(unity_dir) / "Assets" / "Textures"
    materials_dir = Path(unity_dir) / "Assets" / "Materials"
    
    models_dir.mkdir(parents=True, exist_ok=True)
    textures_dir.mkdir(parents=True, exist_ok=True)
    materials_dir.mkdir(parents=True, exist_ok=True)
    
    # Copy files
    input_path = Path(input_dir)
    
    # Copy OBJ files
    for obj_file in input_path.glob("*.obj"):
        print(f"Copying {obj_file.name} to {models_dir}")
        subprocess.run(["cp", str(obj_file), str(models_dir)])
    
    # Copy MTL files
    for mtl_file in input_path.glob("*.mtl"):
        print(f"Copying {mtl_file.name} to {materials_dir}")
        subprocess.run(["cp", str(mtl_file), str(materials_dir)])
    
    # Copy texture files
    for texture_file in input_path.glob("*.png"):
        print(f"Copying {texture_file.name} to {textures_dir}")
        subprocess.run(["cp", str(texture_file), str(textures_dir)])
    
    return True

def create_unity_import_script(unity_dir, scene_name):
    """Create Unity import script."""
    script_content = f'''
using UnityEngine;
using UnityEditor;

public class ImportNeRFScene : Editor
{{
    [MenuItem("Tools/Import NeRF Scene")]
    public static void ImportScene()
    {{
        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // Import mesh
        var meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/{scene_name}.obj");
        if (meshPrefab != null)
        {{
            var meshObject = PrefabUtility.InstantiatePrefab(meshPrefab) as GameObject;
            meshObject.name = "{scene_name}";
            
            // Add collider
            var collider = meshObject.AddComponent<MeshCollider>();
            collider.convex = false;
            
            // Set up lighting
            var light = new GameObject("Directional Light");
            light.AddComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            
            // Set up camera
            var camera = Camera.main;
            if (camera != null)
            {{
                camera.transform.position = new Vector3(0, 2, -5);
                camera.transform.LookAt(Vector3.zero);
            }}
            
            Debug.Log("NeRF scene imported successfully!");
        }}
        else
        {{
            Debug.LogError("Could not find mesh asset: Assets/Models/{scene_name}.obj");
        }}
    }}
}}
'''
    
    script_path = Path(unity_dir) / "Assets" / "Editor" / "ImportNeRFScene.cs"
    script_path.parent.mkdir(parents=True, exist_ok=True)
    
    with open(script_path, 'w') as f:
        f.write(script_content)
    
    print(f"Created Unity import script: {script_path}")
    return True

def main():
    parser = argparse.ArgumentParser(description='Convert NeRF mesh to Unity-ready assets')
    parser.add_argument('--input', required=True, help='Input PLY file')
    parser.add_argument('--output', required=True, help='Output directory')
    parser.add_argument('--unity', help='Unity project directory')
    parser.add_argument('--scene-name', default='nerf_scene', help='Scene name for Unity')
    
    args = parser.parse_args()
    
    # Validate input file
    if not os.path.exists(args.input):
        print(f"Error: Input file {args.input} does not exist")
        sys.exit(1)
    
    # Create output directory
    os.makedirs(args.output, exist_ok=True)
    
    # Get base name for files
    base_name = os.path.splitext(os.path.basename(args.input))[0]
    
    # Define file paths
    obj_file = os.path.join(args.output, f"{base_name}.obj")
    mtl_file = os.path.join(args.output, f"{base_name}.mtl")
    collision_file = os.path.join(args.output, f"{base_name}_collision.obj")
    optimized_file = os.path.join(args.output, f"{base_name}_optimized.obj")
    
    print(f"Starting NeRF to Unity pipeline...")
    print(f"Input: {args.input}")
    print(f"Output: {args.output}")
    
    # Step 1: Convert PLY to OBJ
    if not convert_ply_to_obj(args.input, obj_file):
        print("Failed to convert PLY to OBJ")
        sys.exit(1)
    
    # Step 2: Generate collision mesh
    if not generate_collision_mesh(obj_file, collision_file):
        print("Failed to generate collision mesh")
        sys.exit(1)
    
    # Step 3: Optimize mesh
    if not optimize_mesh(obj_file, optimized_file):
        print("Failed to optimize mesh")
        sys.exit(1)
    
    # Step 4: Copy to Unity project
    if args.unity:
        if not copy_to_unity(args.output, args.unity):
            print("Failed to copy files to Unity project")
            sys.exit(1)
        
        # Step 5: Create Unity import script
        if not create_unity_import_script(args.unity, args.scene_name):
            print("Failed to create Unity import script")
            sys.exit(1)
    
    print("Pipeline completed successfully!")
    print(f"Output files:")
    print(f"  - OBJ: {obj_file}")
    print(f"  - MTL: {mtl_file}")
    print(f"  - Collision: {collision_file}")
    print(f"  - Optimized: {optimized_file}")
    
    if args.unity:
        print(f"Unity project updated: {args.unity}")

if __name__ == "__main__":
    main()
