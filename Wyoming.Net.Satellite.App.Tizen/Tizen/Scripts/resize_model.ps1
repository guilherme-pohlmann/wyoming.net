# This is used to convert the onnx version of melspectrogram into a statically shaped tflite model, which is what Tizen OneShot engine accepts
# onnx2tf -i .\melspectrogram.onnx -o tf_mel --batch_size 1 --overwrite_input_shape "input:1,1760"
