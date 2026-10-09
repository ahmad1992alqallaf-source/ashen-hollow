# raise both upper arms toward the T-pose in world space, bend the right knee forward
for side, s in (('l', 1), ('r', -1)):
    aim('upperarm_' + side, (s * 0.2, 0.05, -1.0)); aim('lowerarm_' + side, (s * 0.1, -0.3, -1.0))
pb = arm.pose.bones['calf_r']; bpy.context.view_layer.update()
T = mathutils.Matrix.Translation(pb.head); R = mathutils.Matrix.Rotation(math.radians(45), 4, 'X')
pb.matrix = T @ R @ T.inverted() @ pb.matrix; bpy.context.view_layer.update()
for nm, deg in (('thigh_l', -45), ('thigh_r', 30)):
    pb = arm.pose.bones[nm]; bpy.context.view_layer.update()
    T = mathutils.Matrix.Translation(pb.head); R = mathutils.Matrix.Rotation(math.radians(deg), 4, 'X')
    pb.matrix = T @ R @ T.inverted() @ pb.matrix; bpy.context.view_layer.update()
