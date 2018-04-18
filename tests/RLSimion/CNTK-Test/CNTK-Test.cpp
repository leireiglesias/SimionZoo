// CNTK-Test.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <iostream>
#include "CNTKLibrary.h"
#include <unordered_map>
using namespace std;

int main()
{
	const size_t input1Dim = 2;
	const size_t input2Dim = 2;
	const size_t outputDim = input1Dim * input2Dim;

	CNTK::Variable input1 = CNTK::InputVariable({ input1Dim }, CNTK::DataType::Double, true, L"state");
	CNTK::Variable input2 = CNTK::InputVariable({ input2Dim }, CNTK::DataType::Double, true, L"action");
	//CNTK::FunctionPtr timesInput1 = CNTK::Times(input1, CNTK::Constant::Scalar(3.0),L"times");
	CNTK::FunctionPtr matTimes = CNTK::Times(input1, input2, L"Sum");
	CNTK::FunctionPtr output = CNTK::Sin(matTimes,L"Output");
	vector<double> outputVector = vector<double>(outputDim);

	//test evaluating the output function
	unordered_map<CNTK::Variable, CNTK::ValuePtr> args = unordered_map<CNTK::Variable, CNTK::ValuePtr>();
	vector<double> input1Data = vector<double>(input1Dim);
	input1Data[0] = 1.0;
	input1Data[1] = 1.52;
	vector<double> input2Data = vector<double>(input2Dim);
	input2Data[0] = 0.0;
	input2Data[1] = 2;
	CNTK::ValuePtr outputValuePtr;
	unordered_map<CNTK::Variable, CNTK::ValuePtr> out = unordered_map<CNTK::Variable, CNTK::ValuePtr>();
	args[input1] = CNTK::Value::CreateBatch({ input1Dim }, input1Data, CNTK::DeviceDescriptor::UseDefaultDevice());
	args[input2] = CNTK::Value::CreateBatch({ input2Dim }, input2Data, CNTK::DeviceDescriptor::UseDefaultDevice());
	out[output] = nullptr;

	output->Evaluate(args, out);
	outputValuePtr = out[output];
	size_t totalSize = output->Output().Shape().TotalSize();
	wcout << L"Function Output Shape: " << output->Output().Shape().AsString() << L"\n";
	wcout << L"Evaluation Output Shape: " << outputValuePtr->Shape().AsString() << L"\n";

	CNTK::NDArrayViewPtr cpuArrayOutput = CNTK::MakeSharedObject<CNTK::NDArrayView>(outputValuePtr->Shape() , outputVector, false);
	outputValuePtr = out[output];
	cpuArrayOutput->CopyFrom(*outputValuePtr->Data());
	wcout << L"Evaluating function= [ ";
	for (size_t i = 0; i<outputVector.size(); i++)
		wcout << outputVector[i] << L" ";
	wcout << L"]\n";
	//test gradient calculation
	unordered_map<CNTK::Variable, CNTK::ValuePtr> arguments= unordered_map<CNTK::Variable, CNTK::ValuePtr>();
	unordered_map<CNTK::Variable, CNTK::ValuePtr> gradients = unordered_map<CNTK::Variable, CNTK::ValuePtr>();

	arguments[input1] = CNTK::Value::CreateBatch({ (size_t)input1Dim }, input1Data, CNTK::DeviceDescriptor::UseDefaultDevice());
	arguments[input2] = CNTK::Value::CreateBatch({ (size_t)input2Dim }, input2Data, CNTK::DeviceDescriptor::CPUDevice());

	gradients[input2] = nullptr;// CNTK::Value::CreateBatch({ (size_t)2 }, outputValues, CNTK::DeviceDescriptor::CPUDevice());
	output->Gradients(arguments, gradients);
	
	vector<double> gradientVector = vector<double>(input2Dim);
	CNTK::ValuePtr input2Gradient = gradients[input2];
	CNTK::NDArrayViewPtr cpuArrayOutput2 =
		CNTK::MakeSharedObject<CNTK::NDArrayView>(input2Gradient->Shape(), gradientVector, false);
	cpuArrayOutput2->CopyFrom(*input2Gradient->Data());

	wcout << L"Gradient wrt input2= [ ";
	for (size_t i = 0; i<gradientVector.size(); i++)
		wcout << gradientVector[i] << L" ";
	wcout << L"]\n";

    return 0;
}

