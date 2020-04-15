from flask import Flask
from flask import request
from flask_cors import CORS, cross_origin
import numpy as np
import cv2
import base64
import face_recognition
import pickle
from imutils import paths
from os import listdir
import os
total=1
step = 0
encoding="encodings.pickle"
image="data_thanh/Hoang Thanh/sample26.png"
detection_method="cnn"
data = pickle.loads(open(encoding, "rb").read())
my_port = '8000'
dataset="test"
def encode():
    global step
    global total
    # encode_face("data_thanh")
    dataset = "test"
    encode = "encodings.pickle"
    detection_method = "cnn"

    # grab the paths to the input images in our dataset
    print("[INFO] quantifying faces...")
    imagePaths = list(paths.list_images(dataset))

    # initialize the list of known encodings and known names
    knownEncodings = []
    knownNames = []
    total=len(imagePaths)
    # loop over the image paths
    for (i, imagePath) in enumerate(imagePaths):
        # extract the person name from the image path
        print("[INFO] processing image {}/{}".format(i + 1,
                                                     len(imagePaths)))
        name = imagePath.split(os.path.sep)[-2]
        step = step+1
        # load the input image and convert it from RGB (OpenCV ordering)
        # to dlib ordering (RGB)

        image = cv2.imread(imagePath)

        print(image.shape)

        rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

        # detect the (x, y)-coordinates of the bounding boxes
        # corresponding to each face in the input image
        boxes = face_recognition.face_locations(rgb,
                                                model=detection_method)

        # compute the facial embedding for the face
        encodings = face_recognition.face_encodings(rgb, boxes)

        # loop over the encodings
        for encoding in encodings:
            # add each encoding + name to our set of known names and
            # encodings
            knownEncodings.append(encoding)

            knownNames.append(name)

    # dump the facial encodings + names to disk
    print("[INFO] serializing encodings...")
    data = {"encodings": knownEncodings, "names": knownNames}

    f = open(encode, "wb")
    f.write(pickle.dumps(data))
    f.close()

def build_return(name, x, y, x_plus_w, y_plus_h):
    return str(name) + "," + str(x) + "," + str(y) + "," + str(x_plus_w) + "," + str(y_plus_h)

def checking():
    percent=(step/total)*100
    if percent>100:
        print("xxxxxxxxxxxxxxxxxxxxxxx")
    return str(int(percent))

# Doan ma khoi tao server
app = Flask(__name__)
CORS(app)

# Khai bao ham xu ly request index
@app.route('/')
@cross_origin()
def index():
    return "Welcome to flask API!"

# Khai bao ham xu ly request hello_word
@app.route('/hello_world', methods=['GET'])
@cross_origin()
def hello_world():
    # Lay staff id cua client gui len
    staff_id = request.args.get('staff_id')
    # Tra ve cau chao Hello
    return "Hello "  + str(staff_id)


@app.route('/train',methods=['GET'])
def train():
    global step,total
    step=0
    total=1
    # t = threading.Thread(encode())
    # t.start()
    # t.join()
    encode()
    return str("TRAINED !")


@app.route('/progress',methods=['GET'])
def check():
    k=checking()
    return str(k)


@app.route('/capture',methods=['POST'])
def detect():
    image_b64 = request.form.get('image')
    name=request.args.get("name")
    id=request.args.get("id")
    pic=request.args.get("pic")
    print(id+" : "+name)
    image = np.fromstring(base64.b64decode(image_b64), dtype=np.uint8)
    image = cv2.imdecode(image, cv2.IMREAD_ANYCOLOR)
    print(image.shape)
    rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

    # detect the (x, y)-coordinates of the bounding boxes corresponding
    # to each face in the input image, then compute the facial embeddings
    # for each face
    print("[INFO] detecting faces...")
    boxes = face_recognition.face_locations(rgb,
                                           model=detection_method)  # tra ve so face trong hinh
    print(len(boxes))
    retString = ""
    if len(boxes)!=0:
        data_dir=os.path.sep.join([dataset,id+"_"+name])
        for dir in listdir(dataset):
            if int(id) == int(dir.split("_")[0]) and name!=dir.split("_")[1]:      #same id, diferent name, break
                return build_return(0, 0,2,3,4)   #id existed

        if not os.path.exists(data_dir):
            os.makedirs(data_dir)
        img_name=name+'_'+pic+".png"
        imgPath=os.path.sep.join([data_dir,img_name])
        cv2.imwrite(imgPath,image)
        print("Save {} successfully !!!".format(img_name))

        for (top, right, bottom, left) in boxes:
            x = left
            y = top
            w = right - left
            h = bottom - top
            # Xay dung chuoi tra ve client
            retString = build_return(len(boxes), round(x), round(y), round(w), round(h))
        return retString
    else:
        retString = build_return(0, 1,2,3,4)
        return retString


# Khai bao ham xu ly request detect
@app.route('/detect', methods=['POST'])
@cross_origin()
def recog():
    image_b64 = request.form.get('image')
    image = np.fromstring(base64.b64decode(image_b64), dtype=np.uint8)
    image = cv2.imdecode(image, cv2.IMREAD_ANYCOLOR)
    print(image.shape)
    #image = imutils.resize(image, width=800)
    # image = cv2.copyMakeBorder(image, 100, 100, 100, 100, borderType=cv2.BORDER_CONSTANT)
    rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

    # detect the (x, y)-coordinates of the bounding boxes corresponding
    # to each face in the input image, then compute the facial embeddings
    # for each face
    print("[INFO] recognizing faces...")
    boxes = face_recognition.face_locations(rgb,
                                            model=detection_method)  # tra ve so face trong hinh

    encodings = face_recognition.face_encodings(rgb, boxes)  # tinh embedded feature
    print(np.array(encodings).shape[0])
    # initialize the list of names for each face detected
    names = []

    # loop over the facial embeddings
    for encoding in encodings:
        # attempt to match each face in the input image to our known
        # encodings

        matches = face_recognition.compare_faces(data["encodings"],
                                                 encoding, tolerance=0.4)  # so sanh voi du lieu
        name = "0_Unknown"

        # check to see if we have found a match
        if True in matches:
            # find the indexes of all matched faces then initialize a
            # dictionary to count the total number of times each face
            # was matched
            matchedIdxs = [i for (i, b) in enumerate(matches) if b]
            # b true thì lấy chỉ số ra
            counts = {}

            # loop over the matched indexes and maintain a count for
            # each recognized face face
            for i in matchedIdxs:
                name = data["names"][i]  # duyệt theo cột name

                counts[name] = counts.get(name, 0) + 1  # 0 là default nếu k gọi đc

            # determine the recognized face with the largest number of
            # votes (note: in the event of an unlikely tie Python will
            # select first entry in the dictionary)
            name = max(counts, key=counts.get)  # tim name co count max

        # update the list of names
        names.append(name)
    retString = ""
    # loop over the recognized faces
    for ((top, right, bottom, left), name) in zip(boxes, names):
        x = left
        y = top
        w = right-left
        h = bottom-top
        # Xay dung chuoi tra ve client
        print(name)
        retString = build_return(name, round(x), round(y), round(w), round(h))

    return retString

# Thuc thi server
if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0',port=my_port)