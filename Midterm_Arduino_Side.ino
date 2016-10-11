/*HW 8, 9, Midterm Arduino code
  Programmer: Ryan Pizzirusso
  Due Date:   Oct. 10-12, 2016*/

#include<AFMotor.h>
#include<Servo.h>

Servo base;
int recieved = 0;

void setup() {
  // put your setup code here, to run once:
  //attach servo to required port, start at initial angle, and start serial communication
  Serial.begin(115200);
  
  base.attach(10);
  base.write(90 / 1.2);
}//end setup

void loop() {
  // put your main code here, to run repeatedly:
  if (Serial.available() > 0){
    recieved = Serial.read(); //get angle from computer
    float command = recieved / 1.2; //servos tend to take the angle sent to them and go to a position 1.2 times that input.  divide by 1.2 to compensate
    base.write(command);  //execute command
    //base.write(recieved);
    recieved = 0;
  }//end if serial available
  
//  Serial.end();
//  Serial.begin(115200);
}//end loop
