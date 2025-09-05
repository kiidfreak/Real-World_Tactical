# NeRF to Unity Pipeline

## Overview

This pipeline automates the conversion of NeRF reconstructions from Instant-NGP into Unity-compatible game environments with proper lighting, materials, and collision meshes.

## Pipeline Steps

### 1. NeRF Training and Export
```bash
# Train NeRF model
./instant-ngp data/nerf/your_scene

# Export mesh
# In Instant-NGP GUI: File -> Export Mesh -> Save as .ply
```

### 2. Mesh Processing
```bash
# Convert to OBJ format
python convert_ply_to_obj.py input.ply output.obj

# Generate collision mesh
python generate_collision_mesh.py input.obj collision.obj

# Optimize for Unity
python optimize_mesh.py input.obj optimized.obj
```

### 3. Unity Import
```bash
# Copy files to Unity project
cp *.obj unity-project/Assets/Models/
cp *.png unity-project/Assets/Textures/

# Run Unity import script
python unity_import_script.py
```

## Scripts

### convert_ply_to_obj.py
Converts PLY mesh files to OBJ format with proper material assignments.

### generate_collision_mesh.py
Creates simplified collision meshes for physics interactions.

### optimize_mesh.py
Optimizes meshes for real-time rendering in Unity.

### unity_import_script.py
Automates Unity asset import and scene setup.

## Usage

1. **Train your NeRF model** using Instant-NGP
2. **Export the mesh** as PLY file
3. **Run the conversion pipeline**:
   ```bash
   python run_pipeline.py --input scene.ply --output unity-project/
   ```
4. **Import into Unity** and configure materials/lighting

## Requirements

- Python 3.8+
- Open3D
- NumPy
- Unity 2022.3 LTS
- Instant-NGP

## Output

- Optimized OBJ mesh files
- Collision meshes
- Material files
- Unity scene setup
- Lighting configuration
