// CNTK-Test.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <iostream>
#include "CNTKLibrary.h"
#include <unordered_map>
using namespace std;
using namespace CNTK;

void PrintParameters(FunctionPtr function)
{
	for (size_t i = 0; i < function->Parameters().size(); i++)
	{
		vector<double> cpuVector = vector<double>(function->Parameters()[i].Shape().TotalSize());
		NDArrayViewPtr cpuParameterArray = MakeSharedObject<NDArrayView>(function->Parameters()[i].Shape(), cpuVector);
		cpuParameterArray->CopyFrom(*(function->Parameters()[i]).Value());
		for (int j = 0; j < cpuVector.size(); j++)
			wcout << L"Parameter#" << i << L": " << cpuVector[j] << L"\n";
	}
	wcout << L"\n";
}

void PrintParameters(const vector<Parameter>& parameters)
{
	for (size_t i = 0; i < parameters.size(); i++)
	{
		vector<double> cpuVector = vector<double>(parameters[i].Shape().TotalSize());
		NDArrayViewPtr cpuParameterArray = MakeSharedObject<NDArrayView>(parameters[i].Shape(), cpuVector);
		cpuParameterArray->CopyFrom(*(parameters[i].Value()));
		for (int j = 0; j < cpuVector.size(); j++)
			wcout << L"Parameter " << parameters[i].Name() << L" : " << cpuVector[j] << L"\n";
	}
	wcout << L"\n";
}

void PrintValuePtr(ValuePtr valuePtr)
{
	vector<double> cpuVector = vector<double>(valuePtr->Shape().TotalSize());
	NDArrayViewPtr cpuArrayView =
		MakeSharedObject<NDArrayView>(valuePtr->Shape(), cpuVector, false);
	cpuArrayView->CopyFrom(*valuePtr->Data());

	wcout << L"[ ";
	for (size_t i = 0; i<cpuVector.size(); i++)
		wcout << cpuVector[i] << L" ";
	wcout << L"]\n";
}

void PrintEvaluationResult(ValuePtr outputValuePtr)
{
	size_t outputDim = outputValuePtr->Shape().TotalSize();
	vector<double> outputVector = vector<double>(outputDim);

	NDArrayViewPtr cpuArrayOutput = MakeSharedObject<NDArrayView>(outputValuePtr->Shape(), outputVector, false);
	cpuArrayOutput->CopyFrom(*outputValuePtr->Data());
	wcout << L"Function evaluation  = [";
	for (size_t i = 0; i<outputVector.size(); i++)
		wcout << outputVector[i] << L" ";
	wcout << L"]\n";
}


int main()
{

	//Play environment to help me understand what I'm doing with gradients and the other stuff
	//  -s: only a scalar
	//  -a: only a scalar
	//Q(s,a) = s*a		, meaning that, the greater the action is and the greater the state is, the greater the state-action value
	//pi(s) = w*s + b	,where 'w' and 'b' are the learnable parameters
	const size_t stateDim = 1;
	const size_t actionDim = 1;
	double s_0 = 2.0;
	double a_0 = 1.0;
	const size_t outputDim = stateDim * actionDim;

	Variable Q_input_s = InputVariable({ stateDim }, DataType::Double, true, L"s");
	Variable Q_input_a = InputVariable({ actionDim }, DataType::Double, true, L"a");
	
	FunctionPtr QFunction = Minus(Q_input_s, Q_input_a, L"Q(s,a)");

	Variable Policy_input_s = InputVariable({ stateDim }, DataType::Double, true, L"s");
	Variable Policy_mult = Parameter({ stateDim }, DataType::Double, 10.0, DeviceDescriptor::UseDefaultDevice(),L"policy-mult");
	Variable Policy_bias = Parameter({ stateDim }, DataType::Double, 0.5, DeviceDescriptor::UseDefaultDevice(),L"policy-bias");
	FunctionPtr inputScale = ElementTimes(Policy_input_s, Policy_mult, L"scale");
	FunctionPtr Policy = Plus(Policy_bias, inputScale, L"pi(s)");

	LearnerPtr PolicyLearner = AdamLearner(Policy->Parameters()
		, LearningRatePerSampleSchedule(0.001)
		, MomentumPerSampleSchedule(0.999));
	//test evaluating the QFunction function
	unordered_map<Variable, ValuePtr> QEvalArgs = unordered_map<Variable, ValuePtr>();
	vector<double> input1Data = vector<double>(stateDim);
	input1Data[0] = s_0;
	vector<double> input2Data = vector<double>(actionDim);
	input2Data[0] = a_0;
	ValuePtr outputValuePtr;
	unordered_map<Variable, ValuePtr> QEvalOut = unordered_map<Variable, ValuePtr>();
	QEvalArgs[Q_input_s] = Value::CreateBatch({ stateDim }, input1Data, DeviceDescriptor::UseDefaultDevice());
	QEvalArgs[Q_input_a] = Value::CreateBatch({ actionDim }, input2Data, DeviceDescriptor::UseDefaultDevice());
	QEvalOut[QFunction] = nullptr;

	QFunction->Evaluate(QEvalArgs, QEvalOut);
	outputValuePtr = QEvalOut[QFunction];

	wcout << L"Q Evaluation:\n";
	PrintEvaluationResult(outputValuePtr);

	//test gradient calculation
	unordered_map<Variable, ValuePtr> qGradArgs= unordered_map<Variable, ValuePtr>();
	unordered_map<Variable, ValuePtr> qGradOutput = unordered_map<Variable, ValuePtr>();

	qGradArgs[Q_input_s] = Value::CreateBatch({ (size_t)stateDim }, input1Data, DeviceDescriptor::UseDefaultDevice());
	qGradArgs[Q_input_a] = Value::CreateBatch({ (size_t)actionDim }, input2Data, DeviceDescriptor::UseDefaultDevice());

	qGradOutput[Q_input_a] = nullptr;
	QFunction->Gradients(qGradArgs, qGradOutput);

	wcout << L"Q gradients:\n";
	PrintValuePtr(qGradOutput[Q_input_a]);

	//Policy update
	wcout << L"Policy parameters:\n";
	PrintParameters(Policy);

	wcout << L"Policy evaluation:\n";
	unordered_map<Variable, ValuePtr> PolicyEvalArgs = {};
	vector<double> stateData = vector<double>(stateDim);
	stateData[0] = 0.75;
	unordered_map<Variable, ValuePtr> PolicyEvalOut = {};
	PolicyEvalArgs[Policy_input_s] = Value::CreateBatch({ stateDim }, stateData, DeviceDescriptor::UseDefaultDevice());
	PolicyEvalOut[Policy] = nullptr;
	Policy->Evaluate(PolicyEvalArgs, PolicyEvalOut, DeviceDescriptor::UseDefaultDevice());
	PrintEvaluationResult(PolicyEvalOut[Policy]);

	wcout << L"\nUpdating the actor using critic gradient\n\n";
	wcout << L"Forward pass\n";
	unordered_map<Variable, ValuePtr> policyFwdArgs = {};
	unordered_map<Variable, ValuePtr> policyFwdOutput = {};
	policyFwdArgs[Policy_input_s] = Value::CreateBatch({ stateDim }, input1Data, DeviceDescriptor::UseDefaultDevice());
	policyFwdOutput[Policy] = nullptr;
	auto backPropState= Policy->Forward(policyFwdArgs, policyFwdOutput, DeviceDescriptor::UseDefaultDevice(), { Policy });
	unordered_map<Variable, ValuePtr> parameterGradients = {};
	for (const Parameter& parameter : PolicyLearner->Parameters())
		parameterGradients[parameter] = nullptr;

	
	//Copy the q gradient to a Cpu vector: qGradientCpuVector
	ValuePtr qGradient = qGradOutput[Q_input_a];
	vector<double> qGradientCpuVector = vector<double>(qGradient->Shape().TotalSize());
	NDArrayViewPtr qParameterGradientCpuArrayView =
		MakeSharedObject<NDArrayView>(qGradient->Shape(), qGradientCpuVector, false);
	qParameterGradientCpuArrayView->CopyFrom(*qGradient->Data());
	assert(qGradient->Shape().TotalSize() == 1);

	
	auto PolicyRootGradientValue = MakeSharedObject<Value>(MakeSharedObject<NDArrayView>(
		DataType::Double, Policy->Output().Shape(), DeviceDescriptor::UseDefaultDevice()));
	PolicyRootGradientValue->Data()->SetValue(-qGradientCpuVector[0]);
	wcout << L"Backward pass:";

	Policy->Backward(backPropState, { {Policy, PolicyRootGradientValue } }, parameterGradients);

	wcout << L"Parameter gradients:\n";
	for (const Parameter& parameter : PolicyLearner->Parameters())
		PrintValuePtr(parameterGradients[parameter]);
	
	
	//Modify the parameter gradient
	//for (const Parameter& parameter : PolicyLearner->Parameters())
	//{
	//	ValuePtr policyParameterGradient = parameterGradients[parameter];
	//	
	//	vector<double> policyParameterGradientCpuVector = vector<double>(policyParameterGradient->Shape().TotalSize());
	//	NDArrayViewPtr policyParameterGradientCpuArrayView =
	//		MakeSharedObject<NDArrayView>(policyParameterGradient->Shape(), policyParameterGradientCpuVector, false);

	//	policyParameterGradientCpuArrayView->CopyFrom(*policyParameterGradient->Data());

	//	for (size_t i = 0; i<policyParameterGradientCpuVector.size(); i++)
	//		policyParameterGradientCpuVector[i]*= -qGradientCpuVector[0];
	//	//copy back modified values
	//	policyParameterGradient->Data()->CopyFrom(*policyParameterGradientCpuArrayView);
	//}

	wcout << L"Parameter gradients *(-dQ/da):\n";
	for (const Parameter& parameter : PolicyLearner->Parameters())
		PrintValuePtr(parameterGradients[parameter]);

	wcout << L"Policy parameters:\n";
	PrintParameters(PolicyLearner->Parameters());

	//update parameters
	wcout << L"Update policy parameters: \n";
	std::unordered_map<Parameter, NDArrayViewPtr> gradients = {};
	for (const auto& parameter : PolicyLearner->Parameters())
		gradients[parameter] = parameterGradients[parameter]->Data();
	bool updated= PolicyLearner->Update(gradients, 1, false);

	wcout << L"Policy parameters:\n";
	PrintParameters(PolicyLearner->Parameters());

	wcout << L"\n\nPress any key...";
	char c= getchar();
    return 0;
}

